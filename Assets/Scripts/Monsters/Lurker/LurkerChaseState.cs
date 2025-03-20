using Pathfinding;
using UnityEngine;

public class LurkerChaseState : LurkerBaseState
{
    private FollowerEntity agent;
    private LurkerAnimationManager anim;
    private float chaseTimer;
    private Vector3 targetPosition;
    private float nextChangeTime;
    public override void EnterState(LurkerMonsterScript lurker)
    {
        targetPosition = lurker.targetPosition;
        nextChangeTime = 0f;
        chaseTimer = 0;
        agent = lurker.agent;
        anim = lurker.animationManager;
    }

    public override void UpdateState(LurkerMonsterScript lurker)
    {
        targetPosition = lurker.targetPosition;
        
		agent.stopDistance = lurker.chasingStoppingDistance;
    	agent.maxSpeed = lurker.GetSpeed();
    	agent.canMove = true;
		
		//Set animations
		anim.PlayRunningAnimation();
		if (Time.time >= nextChangeTime)
        {
			
            float random = Random.value;
            if(random < 0.5f)
            {
                anim.PlayHeadTwitches();
            }else
            {
                anim.PlayArmTwitches();
            }
            // Schedule the next random weight change
            nextChangeTime = Time.time + Random.Range(1, 2.5f);
        }
		
		//allow agent to traverse all tags
		SetPathfindingConstraints(false, -1);
		
		//set agent follow target
		agent.destination = lurker.targetPosition;
		agent.SearchPath();
		
		//if chase timer is exceeded and agent cant see player and distance is far enough, lose aggression and go back to roam state
		chaseTimer += Time.deltaTime;

		if(IsTargetOutOfRange(lurker) && chaseTimer >= lurker.minimumChaseTime)
		{
			lurker.StartStalkCooldown();
			lurker.SwitchState(LurkerState.Roaming);
		}
    }
    
    private void SetPathfindingConstraints(bool constrainTags, int tagMask)
    {
        NNConstraint constraint = NNConstraint.Walkable;
    	constraint.constrainTags = constrainTags;
   	 	constraint.tags = tagMask; //only allow constraint to pick nodes with this tag
    	agent.pathfindingSettings.traversableTags = tagMask; //only allow agent to move through nodes with this tag
    }
    
    private bool IsTargetOutOfRange(LurkerMonsterScript lurker)
	{
	    float targetDistance = Vector3.Distance(lurker.transform.position, targetPosition);
		Vector3 directionToTarget = (targetPosition - lurker.transform.position).normalized;
		return targetDistance >= lurker.stopChasingDistance && !lurker.IsTargetVisible(directionToTarget, targetDistance);
	}
}
