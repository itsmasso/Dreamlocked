
using UnityEngine;
using Unity.Netcode;
using Pathfinding;
using System.Linq;
using System.Collections;


/*****************************************************************
 * MannequinMonsterScript
 *****************************************************************
 * Author: Dylan Werelius
 *****************************************************************
 * Description:
    The purpose of this script is to give actions to the mannequin
    monsters. This script will always be checking what state the
    monsters are in from the GFClockManager script. The mannequins
    will either be inactive, or they will be chasing the players.
    When they are in the AWAKENED state, they will lock onto the
    nearest player and start chasing them. If they are in the
    PASSIVE or ACTIVATING state, then they will not be moving. 
    Additionally, if they are seen by light, then they will stop
    moving.
    NOTE: THIS SCRIPT DOES NOT CONTROL THEIR STATE
 *****************************************************************/

public class MannequinMonsterScript : NetworkBehaviour, IAffectedByLight
{
    [Header("Initial Information")]
    // This provides a reference to the GFClockManager script which will tell me what state the monsters are in
    // See the GFClockManager script for more detaisl
    private GFClockManager manager;
    // This variable will give the monster the ability to track the players locations
    [SerializeField] private FollowerEntity agent;
    // This variable will control the threat level of the monsters
    // Because the threat level is actually controlled in the GFClockManager and just read here, I may need to add this network variable to GFClockManager.cs
    // Maybe, maybe not, but if we get a bug related, I would start by looking there
    [SerializeField] public NetworkVariable<MQThreatLevel> threatLevelNetworkState = new NetworkVariable<MQThreatLevel>(MQThreatLevel.PASSIVE);
    [SerializeField] private MonsterScriptableObject mannequinScriptable;

    [Header("Target Properties")]
    private Transform currentTarget;
    [SerializeField] private LayerMask playerLayer;

    [Header("Attack Properties")]
    private const float ACTIVATING_MOVE_SPEED = 1f;
    //private const float AWAKENED_MOVE_SPEED = 5f;
    private const float STOPPING_DISTANCE = 1.5f;
    [SerializeField] private float attackRange;
    [SerializeField] private float attackCooldown;
    private bool canAttack;
    private Coroutine attackCoroutine;

    [Header("Light Properties")]
    // This variable will ensure that the monster stops moving if it is in light
    public NetworkVariable<bool> inLight = new NetworkVariable<bool>(false);
    
    [Header("Optimization Properties")]
    private float callTimer;
    

    private void Start()
    {
        if(!IsServer) return;
        manager = GFClockManager.Instance;
        threatLevelNetworkState.Value = manager.GetMQThreatLevel();
        agent.stopDistance = STOPPING_DISTANCE;
        callTimer = 0;
    }
    private void Update()
    {
        //only server can run this code
        if(!IsServer) return;
        // if there is a change in threatLevel, then update the variable
        if (threatLevelNetworkState.Value != manager.GetMQThreatLevel())
        {
            threatLevelNetworkState.Value = manager.GetMQThreatLevel();
        }

        // Select a target if there is no target already
        callTimer += Time.deltaTime;
        if(callTimer > 0.5f)
        {
            SetClosestPlayerAsTarget();
            callTimer = 0;
        }

        // Basics of Movement
        switch(threatLevelNetworkState.Value)
        {
            case MQThreatLevel.PASSIVE:
                // Debug.Log("Mannequin Threat Level is now: " + threatLevelNetworkState.Value);
                // No moving for you
                agent.canMove = false;
                break;
            case MQThreatLevel.ACTIVATING:
                // Debug.Log("Mannequin Threat Level is now: " + threatLevelNetworkState.Value);
                // Chase currentTarget at ACTIVATING_SPEED
                if (!inLight.Value)
                {
                    MoveToPlayer(ACTIVATING_MOVE_SPEED);
                }
                AttackPlayer();
                break;
            case MQThreatLevel.AWAKENED:
                // Debug.Log("Mannequin Threat Level is now: " + threatLevelNetworkState.Value);
                // Chase currentTarget at AWAKENED_SPEED
                if (!inLight.Value)
                {
                    MoveToPlayer(mannequinScriptable.baseSpeed);
                }
                AttackPlayer();
                break;

            // This really shouldn't ever happen, but here it is just to be safe
            default:
                Debug.Log("ERROR: MannequinMonsterScript.cs - MQThreatLevel is Broken");
                agent.canMove = false;
                break;
        }
    }
    
    private void SetClosestPlayerAsTarget()
	{
	    if(GameManager.Instance.alivePlayers.Count != 0)
	    {
	        currentTarget = GameManager.Instance.alivePlayers.Where(p => Mathf.Abs(p.GetComponent<PlayerController>().GetPlayerGroundedPosition().y - transform.position.y) < 1)
	        .OrderBy(p => Vector3.Distance(p.GetComponent<PlayerController>().GetPlayerGroundedPosition(), transform.position)).FirstOrDefault();
	    
	    	if(currentTarget == null)
	    	{
                Debug.Log("no target on same floor");
                
	    	    currentTarget = GameManager.Instance.alivePlayers.OrderBy(p => Vector3.Distance(p.GetComponent<PlayerController>().GetPlayerGroundedPosition(), transform.position)).FirstOrDefault();
	    	    Debug.Log("current target y pos: " + currentTarget.GetComponent<PlayerController>().GetPlayerGroundedPosition().y);
	    	    Debug.Log("mannequin y pos: " + transform.position.y);
	    	}
	    	SetCurrentTargetClientRpc(currentTarget.GetComponent<NetworkObject>());
        }
	}

    [ClientRpc]
    private void SetCurrentTargetClientRpc(NetworkObjectReference playerObjectRef)
    {
        playerObjectRef.TryGet(out NetworkObject playerObject);
        currentTarget = playerObject.transform;
    }
    private void MoveToPlayer(float speed)
    {
        agent.canMove = true;
        agent.maxSpeed = speed;
        agent.destination = currentTarget.position;
        agent.SearchPath();
    }
    
    private void AttackPlayer()
    {
        if(currentTarget != null)
        {
            if(Vector3.Distance(currentTarget.position, transform.position) <= attackRange && canAttack)
            {
                PlayerHealth playerHealth = currentTarget.GetComponent<PlayerHealth>();
                playerHealth.TakeDamageServerRpc(mannequinScriptable.damage);
                StartAttackCooldown();
            }
        }
    }
    
    private IEnumerator AttackCooldown()
	{
		canAttack = false;
		yield return new WaitForSeconds(attackCooldown);
		canAttack = true;
	}
	
	public void StartAttackCooldown()
	{
	     if(attackCoroutine != null)
				StopCoroutine(attackCoroutine);
		attackCoroutine = StartCoroutine(AttackCooldown());
	}
	
    public void EnteredLight() 
    {
        if(IsServer)
        {
            inLight.Value = true;
            agent.canMove = false;
            agent.SetPath(null);
        }
        
    }
	public void ExitLight() 
    {
        if (IsServer && threatLevelNetworkState.Value != MQThreatLevel.PASSIVE)
        {
            inLight.Value = false;
            agent.canMove = true;
        }
        
    }

}

