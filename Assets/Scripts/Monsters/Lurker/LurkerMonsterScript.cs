using UnityEngine;
using Unity.Netcode;
using Pathfinding;
using System.Linq;
using System.Collections;

public enum LurkerState
{
    Roaming,
    Stalking,
    Prechase,
    Chasing
}
public class LurkerMonsterScript : NetworkBehaviour, IReactToPlayerGaze, IAffectedByLight
{
	[Header("Pathfinder")]
	public FollowerEntity agent;
	
	[Header("Monster States")]
	[SerializeField] public NetworkVariable<LurkerState> networkState = new NetworkVariable<LurkerState>(LurkerState.Roaming);
	public LurkerBaseState currentState;
	public LurkerRoamState roamState = new LurkerRoamState();
	public LurkerStalkState stalkState = new LurkerStalkState();
	public LurkerPrechaseState preChaseState = new LurkerPrechaseState();
	public LurkerChaseState chaseState = new LurkerChaseState();
	
	[Header("Target Properties")]
	public Transform currentTarget;
	public Vector3 targetPosition;
	
	[Header("Roam Properties")]
	public float defaultStoppingDistance;
	public float roamSpeed;
	
	[Header("Stalking Properties")]
	public float chanceToStalkPlayer;
	public float targetStalkRange;
	public float maxStalkTime;
	public float stalkCooldown;
	public bool canStalk;
	public Coroutine stalkCooldownCoroutine;
	
	[Header("Prechase Properties")]
	public float pauseBeforeChasingDuration;
	
	[Header("Chase Properties")]
	[SerializeField] private float aggressionDistance;
	public float chasingStoppingDistance;
	public float stopChasingDistance; //monster stops chasing after a certain distance
	public float minimumChaseTime; 
	[SerializeField] private LayerMask groundLayer;
	[SerializeField] private LayerMask playerLayer;
	[SerializeField] private LayerMask obstacleLayer;
	
	[Header("Light Properties")]
	[SerializeField] private NetworkVariable<bool> inLight = new NetworkVariable<bool>(false);
	[SerializeField] private float inDarkSpeed;
	[SerializeField] private float inLightSpeed;

	[Header("Animation Properties")]
	public Animator anim;
	public NetworkVariable<float> currentAnimSpeed = new NetworkVariable<float>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	public LurkerAnimationManager animationManager;
	[SerializeField] private float rotationSpeed;

	private void Start() {
		if(IsServer)
		{
			canStalk = true;
			SwitchState(LurkerState.Roaming);
		}
	}

    void OnEnable()
    {
        currentAnimSpeed.OnValueChanged += OnAnimSpeedChange;
    }

    void OnDisable()
    {
        currentAnimSpeed.OnValueChanged -= OnAnimSpeedChange;
    }
    
    private void OnAnimSpeedChange(float previousSpeed, float newSpeed)
    {
        if(!IsServer)
        {
            anim.speed = newSpeed;
            //Debug.Log("changed speed: "  + newSpeed);
        }
    }

    public void SwitchState(LurkerState newState)
	{
		if(!IsServer) return;
		networkState.Value = newState;
	    switch(newState)
	    {
	        case LurkerState.Roaming:
	        	currentState = roamState;
	        	break;
	        case LurkerState.Stalking:
	        	currentState = stalkState;
	        	break;
	        case LurkerState.Prechase:
	        	currentState = preChaseState;
	        	break;
	        case LurkerState.Chasing:
	        	currentState = chaseState;
	        	break;   
	    }

	    currentState.EnterState(this);
	    
	}
	
	private void Update() {
		if(!IsServer) return;
		currentState.UpdateState(this);
		SetAnimationSpeed();
		anim.speed = currentAnimSpeed.Value;
		
		if(currentTarget != null)
			targetPosition = currentTarget.GetComponent<PlayerController>().GetPlayerGroundedPosition();
		//RotateTowardsTarget();
	}
	
	private void RotateTowardsTarget()
	{
	    if(currentTarget != null)
	    {
	    	Vector3 direction = currentTarget.position - transform.position;
			if(IsTargetVisible(direction, GetTargetDistance()))
			{	    
				direction.y = 0; // Lock Y-axis rotation
				Quaternion targetRotation = Quaternion.LookRotation(direction);
				transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
			}
	    }
	}
	
	public float GetTargetDistance()
	{
	    return Vector2.Distance(
			new Vector2(targetPosition.x, targetPosition.z), 
			new Vector2(transform.position.x, transform.position.z)
			);
	}
	
    public void ReactToPlayerGaze(NetworkObjectReference playerObjectRef)
	{
		ChaseTargetServerRpc(playerObjectRef);
	}
	
	[ServerRpc(RequireOwnership = false)]
	private void ChaseTargetServerRpc(NetworkObjectReference playerObjectRef)
	{
		playerObjectRef.TryGet(out NetworkObject playerObject);
		float distance = Vector2.Distance(playerObject.transform.position, transform.position);
		if(networkState.Value != LurkerState.Chasing && networkState.Value != LurkerState.Prechase && distance <= aggressionDistance)
		{
			SetCurrentTargetClientRpc(playerObjectRef);
			SwitchState(LurkerState.Prechase);
		}
	}
	
	public void SetRandomPlayerAsTarget()
	{
	    currentTarget = GameManager.Instance.playerTransforms[Random.Range(0, GameManager.Instance.playerTransforms.Count)];
	    SetCurrentTargetClientRpc(currentTarget.GetComponent<NetworkObject>());
	}
	
	[ClientRpc]
	private void SetCurrentTargetClientRpc(NetworkObjectReference playerObjectRef)
	{
		playerObjectRef.TryGet(out NetworkObject playerObject);
	    currentTarget = playerObject.transform;
	}
	
	public bool IsTargetVisible(Vector3 directionToTarget, float targetDistance)
	{
		int obstacleLayers = obstacleLayer.value | groundLayer.value;
		if(Physics.Raycast(Camera.main.transform.position, directionToTarget, out RaycastHit hit, targetDistance + 1))
		{
			//if ray is hitting an obstacle layer and not hitting the player layer
			if(((1 << hit.collider.gameObject.layer) & playerLayer) == 0 && ((1 << hit.collider.gameObject.layer) & obstacleLayers) != 0)
			{
				return false;
			}		
		}
		return true;
	}
	
	public float GetSpeed()
	{
	    return inLight.Value ? inLightSpeed : inDarkSpeed;
	}
	
	private void SetAnimationSpeed()
	{
	    if ((agent.velocity.magnitude < 0.1f || !agent.canMove || agent.reachedEndOfPath || !agent.hasPath) && networkState.Value != LurkerState.Prechase)
		{
			// Freeze animation if not moving
			animationManager.PlayIdleAnimation();
		}else if (networkState.Value == LurkerState.Chasing)
		{
			currentAnimSpeed.Value = inLight.Value ? 0.75f : 2;
		}else
		{
		    // Default animation speed
			currentAnimSpeed.Value = 1;
		}

		
	}
	
	private IEnumerator StalkCooldown()
	{
		canStalk = false;
		yield return new WaitForSeconds(stalkCooldown);
		canStalk = true;
	}
	
	public void StartStalkCooldown()
	{
	     if(stalkCooldownCoroutine != null)
				StopCoroutine(stalkCooldownCoroutine);
		stalkCooldownCoroutine = StartCoroutine(StalkCooldown());
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

}
