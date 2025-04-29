
using UnityEngine;
using Unity.Netcode;
using Pathfinding;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

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
    [SerializeField] private LayerMask groundLayer;

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
    [SerializeField] private float aggroRadius = 10f;

    [Header("Light Properties")]
    // This variable will ensure that the monster stops moving if it is in light
    public NetworkVariable<bool> inLight = new NetworkVariable<bool>(false);
    private HashSet<DetectEnemyInLights> activeLights = new();
    private bool isInLightThisFrame = false;
    private bool wasInLightLastFrame = false;

    [Header("Optimization Properties")]
    private float callTimer;
    [Header("Animation")]
    [SerializeField] private MannequinAnimationManager mannequinAnimationManager;
    [SerializeField] private float openingDoorRange;
    [SerializeField] private LayerMask doorLayer;

    [Header("SFX")]
    private float footstepTimer;
    [SerializeField] private float walkingFootStepInterval;
    [SerializeField] private Sound3DSO[] footStepSounds;
    [SerializeField] private AudioSource footstepAudioSource;
    private void Start()
    {
        if (!IsServer) return;
        manager = GFClockManager.Instance;
        canAttack = true;
        agent.stopDistance = STOPPING_DISTANCE;
        callTimer = 0;
       
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (manager != null) threatLevelNetworkState.Value = manager.GetMQThreatLevel();
    }

    
    private void Update()
    {
        //only server can run this code
        if (!IsServer) return;
        CheckForDoor();
        // if there is a change in threatLevel, then update the variable
        if (threatLevelNetworkState.Value != manager.GetMQThreatLevel())
        {
            threatLevelNetworkState.Value = manager.GetMQThreatLevel();
        }

        if (isInLightThisFrame && !wasInLightLastFrame && threatLevelNetworkState.Value != MQThreatLevel.PASSIVE)
        {
            EnteredLight();
        }

        if (!isInLightThisFrame && wasInLightLastFrame && threatLevelNetworkState.Value != MQThreatLevel.PASSIVE)
        {
            ExitLight();
        }

        wasInLightLastFrame = isInLightThisFrame;
        isInLightThisFrame = false;

        // Basics of Movement
        switch (threatLevelNetworkState.Value)
        {
            case MQThreatLevel.PASSIVE:
                // Debug.Log("Mannequin Threat Level is now: " + threatLevelNetworkState.Value);
                // No moving for you
                mannequinAnimationManager.PlayIdleAnimation();
                agent.canMove = false;
                break;
            case MQThreatLevel.ACTIVATING:
                // Debug.Log("Mannequin Threat Level is now: " + threatLevelNetworkState.Value);
                // Chase currentTarget at ACTIVATING_SPEED

                if (!inLight.Value)
                {
                    MoveToPlayer(ACTIVATING_MOVE_SPEED);
                    mannequinAnimationManager.SetAnimationSpeed(0.5f);
                }
                AttackPlayer();
                break;
            case MQThreatLevel.AWAKENED:
                // Debug.Log("Mannequin Threat Level is now: " + threatLevelNetworkState.Value);
                // Chase currentTarget at AWAKENED_SPEED

                if (!inLight.Value)
                {
                    MoveToPlayer(mannequinScriptable.baseSpeed);

                    mannequinAnimationManager.SetAnimationSpeed(1f);
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

    // private void SetClosestPlayerAsTarget()
    // {
    //     if (!IsServer) return;

    //     if (PlayerNetworkManager.Instance.alivePlayers.Count > 0)
    //     {
    //         currentTarget = PlayerNetworkManager.Instance.alivePlayers.Where(p => Mathf.Abs(p.GetComponent<PlayerController>().GetPlayerGroundedPosition().y - transform.position.y) < 1)
    //         .OrderBy(p => Vector3.Distance(p.GetComponent<PlayerController>().GetPlayerGroundedPosition(), transform.position)).FirstOrDefault().transform;

    //         if (currentTarget == null)
    //         {
    //             Debug.Log("no target on same floor");

    //             currentTarget = PlayerNetworkManager.Instance.alivePlayers.OrderBy(p => Vector3.Distance(p.GetComponent<PlayerController>().GetPlayerGroundedPosition(), transform.position)).FirstOrDefault().transform;
    //         }
    //     }
    // }

    private void FindPlayersInRadius()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, aggroRadius, playerLayer);
        float closestDistance = Mathf.Infinity;
        Transform closestTarget = null;

        foreach (Collider collider in colliders)
        {
            float verticalDifference = Mathf.Abs(transform.position.y - collider.transform.position.y);
            if (verticalDifference > 2f)
                continue; // skip if not on the same floor

            float distance = Vector3.Distance(transform.position, collider.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = collider.transform;
            }
        }

        currentTarget = closestTarget;
    }
    private void CheckForDoor()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, openingDoorRange, doorLayer);
        foreach (Collider collider in colliders)
        {
            Door door = collider.GetComponent<Door>();
            if (door != null)
            {
                door.OpenDoor(transform.position);
            }
        }

    }

    // [Rpc(SendTo.Everyone)]
    // private void SetCurrentTargetRpc(NetworkObjectReference playerObjectRef)
    // {
    //     if(playerObjectRef.TryGet(out NetworkObject playerNetObj))
    //     {
    //         currentTarget = playerNetObj.transform;
    //     }
    // }
    private void MoveToPlayer(float speed)
    {
        // Select a target if there is no target already
        callTimer += Time.deltaTime;
        if (callTimer > 1f)
        {
            //SetClosestPlayerAsTarget();
            FindPlayersInRadius();
            callTimer = 0;
        }
        agent.canMove = true;

        agent.maxSpeed = speed;
        if (currentTarget != null)
        {
            HandleNormalFootStepSFX();
            agent.destination = currentTarget.position;
            mannequinAnimationManager.PlayWalkAnimation();
            agent.SearchPath();
        }

    }

    private void AttackPlayer()
    {
        if (currentTarget != null)
        {
            if (canAttack && Vector3.Distance(currentTarget.position, transform.position) <= attackRange)
            {
                mannequinAnimationManager.PlayAttackAnimation();
                PlayerHealth playerHealth = currentTarget.GetComponent<PlayerHealth>();
                playerHealth.RequestServerTakeDamageRpc(mannequinScriptable.damage);
                StartAttackCooldown();
            }

        }
    }
    public void HandleNormalFootStepSFX()
    {
        footstepTimer -= Time.deltaTime;

        if (footstepTimer <= 0f)
        {
            PlayNormalFootstepRpc();

            float speed = threatLevelNetworkState.Value == MQThreatLevel.ACTIVATING ? ACTIVATING_MOVE_SPEED : mannequinScriptable.baseSpeed;
            float interval = walkingFootStepInterval / Mathf.Max(speed, 0.1f); // prevent division by 0

            footstepTimer = Mathf.Max(interval, 0.2f);
        }
    }

    [Rpc(SendTo.Everyone)]
    public void PlayNormalFootstepRpc()
    {
        Sound3DSO footStep = footStepSounds[Random.Range(0, footStepSounds.Length)];
        footstepAudioSource.pitch = Random.Range(1f, 1.3f);
        footstepAudioSource.volume = Random.Range(Mathf.Clamp01(footStep.volume - 0.4f), footStep.volume);
        footstepAudioSource.minDistance = footStep.minDistance;
        footstepAudioSource.maxDistance = footStep.maxDistance;
        footstepAudioSource.outputAudioMixerGroup = footStep.audioMixerGroup;
        footstepAudioSource.PlayOneShot(footStep.clip, footStep.volume);

    }

    private IEnumerator AttackCooldown()
    {
        canAttack = false;
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    public void StartAttackCooldown()
    {
        if (attackCoroutine != null)
            StopCoroutine(attackCoroutine);
        attackCoroutine = StartCoroutine(AttackCooldown());
    }

    public void EnteredLight()
    {
        if (IsServer)
        {
            inLight.Value = true;
            agent.canMove = false;
            mannequinAnimationManager.SetAnimationSpeed(0);
            agent.SetPath(null);
        }

    }
    public void ExitLight()
    {
        if (IsServer && threatLevelNetworkState.Value != MQThreatLevel.PASSIVE)
        {
            inLight.Value = false;
            agent.canMove = true;
            mannequinAnimationManager.SetAnimationSpeed(1);
        }

    }

    public void AddLightSource(DetectEnemyInLights lightSource)
    {
        if (activeLights.Add(lightSource) && activeLights.Count == 1)
        {
            EnteredLight();
        }
    }

    public void RemoveLightSource(DetectEnemyInLights lightSource)
    {
        if (activeLights.Remove(lightSource) && activeLights.Count == 0)
        {
            ExitLight();
        }
    }

    public void SetInLight(bool isInLight)
    {
        isInLightThisFrame = isInLight;
    }
}

