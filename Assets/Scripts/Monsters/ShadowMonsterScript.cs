using UnityEngine;
using Unity.Netcode;
using Pathfinding;
using System.Linq;
using System.Collections;


public class ShadowMonsterScript : NetworkBehaviour, IReactToPlayerGaze, IAffectedByLight
{
	
	private enum MonsterStates
	{
		Roaming,
		Stalking,
		Prechase,
		Chasing
		

	}
	
	[SerializeField] private NetworkVariable<bool> inLight = new NetworkVariable<bool>(false);
	[Header("Initialize")]
	[SerializeField] private FollowerEntity agent;
	[SerializeField] private NetworkVariable<MonsterStates> currentState = new NetworkVariable<MonsterStates>(MonsterStates.Roaming);
	[SerializeField] private float roamSpeed;
	[SerializeField] private float inDarkSpeed;
	[SerializeField] private float inLightSpeed;
	
	[Header("Stalking Properties")]
	[SerializeField] private float searchRadius; //the search radius for when it needs to check for nodes outside of room
	[SerializeField] private float chanceToStalkPlayer;
	private Transform currentTarget;
	private Vector3 playerPosition;
	[SerializeField] private float playerStalkRange;
	private float stalkTimer;
	[SerializeField] private float maxStalkTime;
	private bool canStalk;
	[SerializeField] private float stalkCooldown;
	private Coroutine stalkCooldownCoroutine;
	private bool chosenDoorToHover;
	private int chosenDoorTries;
	private Vector3 doorToHoverPosition;
	
	[Header("Chase Properties")]
	[SerializeField] private float defaultStoppingDistance;
	[SerializeField] private float chasingStoppingDistance;
	[SerializeField] private float stopChasingDistance; //monster stops chasing after a certain distance
	private float chaseTimer;
	[SerializeField] private float pauseBeforeChasingDuration;
	private float prechaseTimer;
	[SerializeField] private float maxChaseTime; 
	[SerializeField] private LayerMask groundLayer;
	[SerializeField] private LayerMask playerLayer;
	[SerializeField] private LayerMask obstacleLayer;

	

	private void Start() {
		if(IsServer)
		{
			canStalk = true;
			agent.maxSpeed = roamSpeed;
		}

	}
	
	public override void OnNetworkSpawn()
	{

	}
	
	public void ReactToPlayerGaze(NetworkObjectReference playerObjectRef)
	{
		ChaseTargetServerRpc(playerObjectRef);
		
	}
	
	[ServerRpc(RequireOwnership = false)]
	private void ChaseTargetServerRpc(NetworkObjectReference playerObjectRef)
	{
		if(currentState.Value != MonsterStates.Chasing && currentState.Value != MonsterStates.Prechase)
		{
			playerObjectRef.TryGet(out NetworkObject playerObject);
			currentTarget = playerObject.transform;
			currentState.Value = MonsterStates.Prechase;
	
		}
		
	}

	private bool IsPlayerVisible(Vector3 directionToPlayer, float playerDistance)
	{
		int obstacleLayers = obstacleLayer.value | groundLayer.value;
		if(Physics.Raycast(Camera.main.transform.position, directionToPlayer, out RaycastHit hit, playerDistance + 1))
		{
			//if ray is hitting an obstacle layer and not hitting the player layer
			if(((1 << hit.collider.gameObject.layer) & playerLayer) == 0 && ((1 << hit.collider.gameObject.layer) & obstacleLayers) != 0)
			{
				return false;
			}		
		}
		return true;
	}
	
	public void EnteredLight()
	{
		if(IsServer)
			inLight.Value = true;
	}

	public void ExitLight()
	{
		if(IsServer)
			inLight.Value = false;
	}
	
	private IEnumerator StartStalkCooldown()
	{
		canStalk = false;
		yield return new WaitForSeconds(stalkCooldown);
		canStalk = true;
	}
	
	private bool IsValidNode(GraphNode node)
	{
		//tag 1 is room node
		return node.Tag != 1;

	}
	
	
	
	private void Update() {
		if(!IsServer) return;
		NNConstraint constraint = NNConstraint.Walkable;
		//adjusted player position to account for player jumping
		if(currentTarget != null)
			playerPosition = new Vector3(currentTarget.position.x, currentTarget.GetComponent<PlayerController>().floorPosition, currentTarget.position.z);
		switch(currentState.Value)
		{
			case MonsterStates.Roaming:
				//Reset Timers
				stalkTimer = 0;
				chaseTimer = 0;

				//Adjust agent settings
				agent.stopDistance = defaultStoppingDistance;
				agent.canMove = true;
				agent.maxSpeed = roamSpeed;
				
				//Adjust constraints
				constraint.constrainTags = true;
				constraint.tags = 1 << 2; //only allow constraint to pick nodes with this tag
				agent.pathfindingSettings.traversableTags = 1 << 2; //only allow agent to move through nodes with this tag
				
				//Search for random point to roam to
				if (!agent.pathPending && (agent.reachedEndOfPath || !agent.hasPath)) {
						NNInfo sample = AstarPath.active.graphs[0].RandomPointOnSurface(constraint);
						agent.destination = sample.position;
						agent.SearchPath();
				}
				
				//Random chance to switch to stalk state. checks every second
				float chance = Random.value;
				if(chance*100 <= chanceToStalkPlayer * Time.deltaTime && canStalk)
				{
					currentTarget = GameManager.Instance.playerTransforms[Random.Range(0, GameManager.Instance.playerTransforms.Count)];

					currentState.Value = MonsterStates.Stalking;
				}
				
				break;
			case MonsterStates.Stalking:
				//Adjust agent settings
				agent.stopDistance = defaultStoppingDistance;
				agent.maxSpeed = roamSpeed;
				
				//Calculate distance between agent and player
				float targetDistance = Vector2.Distance(new Vector2(playerPosition.x, playerPosition.z), new Vector2(transform.position.x, transform.position.z));
				
				//if target is in a room (1 = room tag)
				if(AstarPath.active.GetNearest(playerPosition).node.Tag == 1)
				{
					//gets random node next to door, returns 0 vector if it cant retrieve the node and sets it to go roam. 
					//Only picks a room position once and will stay there until player leaves the room.
					
					if(!chosenDoorToHover)
					{
						doorToHoverPosition = HouseMapGenerator.Instance.GetRandomDoorNeighbourPos(playerPosition);
						if(doorToHoverPosition != Vector3.zero)
						{
							//Debug.Log(doorToHoverPosition);
							agent.canMove = true;
							agent.destination = doorToHoverPosition;
							agent.SearchPath();
							chosenDoorToHover = true;
						}
						else
						{
							chosenDoorTries++;
							if(chosenDoorTries >= 30f)
							{
								Debug.LogWarning("cant get to player");
							    if(stalkCooldownCoroutine != null)
									StopCoroutine(stalkCooldownCoroutine);
								stalkCooldownCoroutine = StartCoroutine(StartStalkCooldown());
								currentState.Value = MonsterStates.Roaming;	
							}
						}
					}   
				}
				else
				{
					chosenDoorToHover = false;
					//allows movement if agent keeps distance from player and if the node is walkable	
					if(targetDistance > playerStalkRange && IsValidNode(AstarPath.active.GetNearest(agent.position).node))
					{	
						agent.destination = playerPosition;
						agent.SearchPath();
						agent.canMove = true;
					}
					else
					{
						Debug.Log("in range of player");
						agent.canMove = false;
					}
					
				}

				stalkTimer += Time.deltaTime;
				if(stalkTimer >= maxStalkTime)
				{
					if(stalkCooldownCoroutine != null)
						StopCoroutine(stalkCooldownCoroutine);
					stalkCooldownCoroutine = StartCoroutine(StartStalkCooldown());
					currentState.Value = MonsterStates.Roaming;
				}
				
				break;
			case MonsterStates.Prechase:
				//Reset Timers
				stalkTimer = 0;
				agent.canMove = false;
				//play scary animation
				prechaseTimer += Time.deltaTime;
				if(prechaseTimer > pauseBeforeChasingDuration)
				{
				    currentState.Value = MonsterStates.Chasing;
				}

				break;
			case MonsterStates.Chasing:
				//Reset Timers
				prechaseTimer = 0;
				stalkTimer = 0;
				
				//Adjust agent settings
				agent.maxSpeed = inLight.Value ? inLightSpeed : inDarkSpeed;
				agent.stopDistance = chasingStoppingDistance;
				agent.canMove = true;
				//allow agent to traverse all tags
				agent.pathfindingSettings.traversableTags = -1; 
				
				//don't constrain any tags
				constraint.constrainTags = false;
				
				//set agent follow target
				agent.destination = playerPosition;
				agent.SearchPath();
				
				//if chase timer is exceeded and agent cant see player and distance is far enough, lose aggression and go back to roam state
				chaseTimer += Time.deltaTime;
				float playerDistance = Vector3.Distance(transform.position, playerPosition);
				Vector3 directionToPlayer = (playerPosition - transform.position).normalized;
				if(playerDistance >= stopChasingDistance && chaseTimer >= maxChaseTime && !IsPlayerVisible(directionToPlayer, playerDistance))
				{
					if(stalkCooldownCoroutine != null)
						StopCoroutine(stalkCooldownCoroutine);
					stalkCooldownCoroutine = StartCoroutine(StartStalkCooldown());
					
					currentState.Value = MonsterStates.Roaming;
				}
				
				break;
			default:
				break;
		}
		
	}


}
