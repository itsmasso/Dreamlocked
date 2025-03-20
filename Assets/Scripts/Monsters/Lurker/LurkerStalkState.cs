
using Pathfinding;
using UnityEngine;
using System.Collections;
public class LurkerStalkState : LurkerBaseState
{
    private FollowerEntity agent;
    private LurkerAnimationManager anim;
    private Vector3 targetPosition;
    private float playerStalkRange;
    private bool chosenDoorToHover;
    private Vector3 doorToHoverPosition;
    private int chosenDoorTries;
    private int maxTries = 30;
    private float stalkTimer;
    public override void EnterState(LurkerMonsterScript lurker)
    {
        playerStalkRange = lurker.targetStalkRange;
        targetPosition = lurker.targetPosition;
        agent = lurker.agent;
        anim = lurker.animationManager;
        stalkTimer = 0;
    }

    public override void UpdateState(LurkerMonsterScript lurker)
    {
        targetPosition = lurker.targetPosition;
        agent.stopDistance = lurker.defaultStoppingDistance;
        agent.maxSpeed = lurker.roamSpeed;
        agent.canMove = true;
        
		//Set animations
		anim.PlayWalkAnimation();
		
		//if target is in a room (1 = room tag)
		if(IsPlayerInRoom())
		{
			HoverNearPlayerInRoom(lurker);
		}
		else 
		{
			FollowPlayerIfFarEnough(lurker.GetTargetDistance());
		}

		stalkTimer += Time.deltaTime;
		if(stalkTimer >= lurker.maxStalkTime)
		{
			lurker.StartStalkCooldown();
			lurker.SwitchState(LurkerState.Roaming);
		}
    }
    
    private bool IsPlayerInRoom()
	{
	    return AstarPath.active.GetNearest(targetPosition).node.Tag == 1;
	}
	
	private void HoverNearPlayerInRoom(LurkerMonsterScript lurker)
	{
		if(!chosenDoorToHover)
		{
			//gets random node next to door, returns 0 vector if it cant retrieve the node and sets it to go roam. 
			//Only picks a room position once and will stay there until player leaves the room.
			doorToHoverPosition = HouseMapGenerator.Instance.GetDoorClosestToTarget(targetPosition);
			if(doorToHoverPosition != Vector3.zero)
			{
				Debug.Log(doorToHoverPosition);
				agent.canMove = true;
				agent.destination = doorToHoverPosition;
				agent.SearchPath();
				chosenDoorToHover = true;
			}
			else if(++chosenDoorTries >= maxTries)
			{
				Debug.LogWarning("Lurker can't get to the player!");
				lurker.StartStalkCooldown();
			    lurker.SwitchState(LurkerState.Roaming);
			}
		} 
	}
	
	private void FollowPlayerIfFarEnough(float targetDistance) 
	{
		//allows movement if agent keeps distance from player and if the node is walkable
		chosenDoorToHover = false;
		if (targetDistance > playerStalkRange && IsValidNode(AstarPath.active.GetNearest(agent.position).node)) {
			agent.destination = targetPosition;
			agent.SearchPath();
		} else {
			agent.canMove = false;
    	}
	}
	
	private bool IsValidNode(GraphNode node)
	{
		//tag 1 is room node
		return node.Tag != 1;
	}
	
}
