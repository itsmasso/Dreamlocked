using System.Collections;
using Pathfinding;
using UnityEngine;

public class LurkerChaseState : LurkerBaseState
{
    //lurker component variables
    private FollowerEntity agent;
    private LurkerAnimationManager anim;
    //Lurker position
    private Transform lurkerTransform;
    private Vector3 targetPosition;
    //Chasing Variables
    private float chaseTimer;
    private float nextChangeTime;
    private float stopChasingDistance;
    //Attack Variables
    private float attackRange;

    //Layers
    private LayerMask groundLayer;
	private LayerMask playerLayer;
	private LayerMask obstacleLayer;
	//optimization variables
	private float currentCheckTime;
	private float interval;
    public override void EnterState(LurkerMonsterScript lurker)
    {
        //initialize variables
        targetPosition = lurker.targetPosition;
        lurkerTransform = lurker.transform;
        attackRange = lurker.attackRange;
        stopChasingDistance = lurker.stopChasingDistance;
        groundLayer = lurker.groundLayer;
        playerLayer = lurker.playerLayer;
        obstacleLayer = lurker.obstacleLayer;
        agent = lurker.agent;
        anim = lurker.animationManager;
        
        //reset variables
        nextChangeTime = 0f;
        chaseTimer = 0;
        currentCheckTime = 0f;
        interval = 0.2f; //checking 5 times per second

    }

    public override void UpdateState(LurkerMonsterScript lurker)
    {
        targetPosition = lurker.targetPosition;
        
		agent.stopDistance = lurker.chasingStoppingDistance;
    	agent.maxSpeed = lurker.GetSpeed();
    	agent.canMove = true;
		
		//Set animations
		anim.PlayRunningAnimation();
		HandleTwitchAnimations();
		
		//allow agent to traverse all tags
		SetPathfindingConstraints(false, -1);
		
		//set agent follow target
		agent.destination = lurker.targetPosition;
		agent.SearchPath();
		
		//if chase timer is exceeded and agent cant see player and distance is far enough, lose aggression and go back to roam state
		chaseTimer += Time.deltaTime;

		if(IsTargetOutOfRange() && chaseTimer >= lurker.minimumChaseTime)
		{
			lurker.StartStalkCooldown();
			lurker.SwitchState(LurkerState.Roaming);
		}
		
		HandleAttackingPlayer(lurker);
    }
    
    private void HandleTwitchAnimations()
    {
        if (Time.time >= nextChangeTime)
        {
            if(Random.value < 0.75f)
                anim.PlayHeadTwitches();
            
            if(Random.value < 0.5f)
                    anim.PlayLeftArmTwitch();
                    
            if(Random.value < 0.5f)
                anim.PlayRightArmTwitch();
                
            // Schedule the next random weight change
            nextChangeTime = Time.time + Random.Range(1, 1.5f);
        }
    }
    
    private void SetPathfindingConstraints(bool constrainTags, int tagMask)
    {
        NNConstraint constraint = NNConstraint.Walkable;
    	constraint.constrainTags = constrainTags;
   	 	constraint.tags = tagMask; //only allow constraint to pick nodes with this tag
    	agent.pathfindingSettings.traversableTags = tagMask; //only allow agent to move through nodes with this tag
    }
    
    private bool IsTargetOutOfRange()
	{
	    float targetDistance = Vector3.Distance(lurkerTransform.position, targetPosition);
		Vector3 directionToTarget = (targetPosition - lurkerTransform.position).normalized;
		return targetDistance >= stopChasingDistance && !IsTargetVisible(directionToTarget, targetDistance);
	}

	private void HandleAttackingPlayer(LurkerMonsterScript lurker)
	{
	    if(lurker.currentTarget != null)
	    {
	        currentCheckTime += Time.deltaTime;
	        if(currentCheckTime >= interval)
	        {
                if(GetTargetDistance() <= attackRange)
                {
                   lurker.SwitchState(LurkerState.Attacking);
                }
	            currentCheckTime = 0f;
	        }
	    }
	}

	private bool IsTargetVisible(Vector3 directionToTarget, float targetDistance)
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
	
	private float GetTargetDistance()
	{
	    return Vector2.Distance(
			new Vector2(targetPosition.x, targetPosition.z), 
			new Vector2(lurkerTransform.position.x, lurkerTransform.position.z)
			);
	}


    /*
    private void RotateTowardsTarget()
    {
        if(currentTarget != null)
        {
            Vector3 direction = currentTarget.position - lurkerTransform.position;
            if(IsTargetVisible(direction, GetTargetDistance()))
            {	    
                direction.y = 0; // Lock Y-axis rotation
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                lurkerTransform.rotation = Quaternion.Slerp(lurkerTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }
    */
}
