using UnityEngine;
using Unity.Netcode;
using Pathfinding;
using System.Linq;
using System.Collections;


public class ShadowMonsterScript : NetworkBehaviour, IReactToPlayerGaze
{
	
	private enum MonsterStates
	{
		Roaming,
		Stalking,
		Chasing
		

	}
	
	[Header("Initialize")]
	[SerializeField] private FollowerEntity agent;
	[SerializeField] private float minSpawnDistFromPlayers;
	[SerializeField] private float maxSpawnDistFromPlayers;
	private MonsterStates currentState;
	
	[Header("Stalking Properties")]
	[SerializeField] private float chanceToStalkPlayer;
	private Transform currentTarget;
	[SerializeField] private float playerStalkRange;
	private float stalkTimer;
	[SerializeField] private float maxStalkTime;
	private bool canStalk;
	[SerializeField] private float stalkCooldown;
	private Coroutine stalkCooldownCoroutine;
	
	[Header("Chase Properties")]
	[SerializeField] private float defaultStoppingDistance;
	[SerializeField] private float chasingStoppingDistance;
	[SerializeField] private float stopChasingDistance; //monster stops chasing after a certain distance
	private float chaseTimer;
	[SerializeField] private float maxChaseTime; 
	[SerializeField] private LayerMask groundLayer;
	[SerializeField] private LayerMask playerLayer;
	[SerializeField] private LayerMask obstacleLayer;

	

	private void Start() {
		currentState = MonsterStates.Roaming;
		canStalk = true;

	}
	
	public override void OnNetworkSpawn()
	{
		
		transform.position = SpawnAwayFromPlayers();
	}
	
	private Vector3 SpawnAwayFromPlayers()
	{
		Vector3 bestPositionToSpawn = transform.position;
		float maxMinDistance = 0f;
		
		foreach(Vector3 hallwayPos in LevelManager.Instance.levelGenerator.GetHallwayList().ToList()){
			float minDistToPlayers = float.MaxValue;
			
			// Calculate the minimum distance from this hallway position to all players
			foreach(Transform player in PlayerSpawner.Instance.playerTransforms)
			{
				float dist = Vector3.Distance(hallwayPos, player.transform.position);
				if(dist < minDistToPlayers)
				{
					minDistToPlayers = dist;
				}
			}
			
			// If this position meets the minimum distance requirement, return it immediately
			if(minDistToPlayers >= minSpawnDistFromPlayers && minDistToPlayers <= maxSpawnDistFromPlayers)
			{
				Debug.Log("found good position");
				return hallwayPos;
				
			}
				
				
			// Otherwise, track the position that is farthest from the nearest player
			if(minDistToPlayers > maxMinDistance)
			{
				maxMinDistance = minDistToPlayers;
				bestPositionToSpawn = hallwayPos;
				
			}
		}
		Debug.Log("No valid positions");
		// If no valid position was found, return the farthest possible one
		return bestPositionToSpawn;
		
	}
	
	private bool IsOnRestrictedNode()
	{
		// Get node agent is on
		var node = AstarPath.active.GetNearest(agent.position).node;
		if((node.Tag == 1) && currentState != MonsterStates.Stalking)
			return true;
		return false;
	}
	
	public void ReactToPlayerGaze(Transform playerTransform)
	{
		ChaseTarget(playerTransform);
	}
	
	private void ChaseTarget(Transform playerTransform)
	{
		if(currentState != MonsterStates.Chasing)
		{
			currentTarget = playerTransform;
			currentState = MonsterStates.Chasing;
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
	
	private IEnumerator StartStalkCooldown()
	{
		canStalk = false;
		yield return new WaitForSeconds(stalkCooldown);
		canStalk = true;
	}
	
	private void Update() {
		NNConstraint constraint = NNConstraint.Walkable;
		
		switch(currentState)
		{
			case MonsterStates.Roaming:
				//Debug.Log("roaming");

				chaseTimer = 0;
				agent.stopDistance = defaultStoppingDistance;
				agent.canMove = true;
				constraint.constrainTags = true;
				constraint.tags = 1 << 2; //only allow constraint to pick nodes with this tag
				agent.pathfindingSettings.traversableTags = 1 << 2; //only allow agent to move through nodes with this tag
				
				if (!agent.pathPending && (agent.reachedEndOfPath || !agent.hasPath)) {
						NNInfo sample = AstarPath.active.graphs[0].RandomPointOnSurface(constraint);
						agent.destination = sample.position;
						agent.SearchPath();
					}
				float chance = Random.value;
				if(chance*100 <= chanceToStalkPlayer * Time.deltaTime && canStalk)
				{
					currentTarget = PlayerSpawner.Instance.playerTransforms[Random.Range(0, PlayerSpawner.Instance.playerTransforms.Count)];
					currentState = MonsterStates.Stalking;
				}
				break;
			case MonsterStates.Stalking:
				//Debug.Log("stalking");

				agent.stopDistance = defaultStoppingDistance;
				float targetDistance = Vector2.Distance(new Vector2(currentTarget.position.x, currentTarget.position.z), new Vector2(transform.position.x, transform.position.z));
				if(targetDistance > playerStalkRange && !IsOnRestrictedNode())
				{
					agent.destination = currentTarget.position;
					agent.SearchPath();
					agent.canMove = true;
				}
				else
				{
					agent.canMove = false;
				}
				stalkTimer += Time.deltaTime;
				if(stalkTimer >= maxStalkTime)
				{
					if(stalkCooldownCoroutine != null)
						StopCoroutine(stalkCooldownCoroutine);
					stalkCooldownCoroutine = StartCoroutine(StartStalkCooldown());
					
					currentState = MonsterStates.Roaming;
				}
				
				break;
			case MonsterStates.Chasing:
				//Debug.Log("chasing");

				agent.stopDistance = chasingStoppingDistance;
				agent.canMove = true;
				constraint.constrainTags = false;
				agent.pathfindingSettings.traversableTags = -1; 
				
				agent.destination = currentTarget.position;
				agent.SearchPath();
				chaseTimer += Time.deltaTime;
				
				float playerDistance = Vector3.Distance(transform.position, currentTarget.position);
				Vector3 directionToPlayer = (currentTarget.position - transform.position).normalized;
				if(playerDistance >= stopChasingDistance && chaseTimer >= maxChaseTime && !IsPlayerVisible(directionToPlayer, playerDistance))
				{
					Debug.Log("de aggroed");
					if(stalkCooldownCoroutine != null)
						StopCoroutine(stalkCooldownCoroutine);
					stalkCooldownCoroutine = StartCoroutine(StartStalkCooldown());
					
					currentState = MonsterStates.Roaming;
				}
				break;

		}
		
	}
}
