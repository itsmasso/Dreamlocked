using System.Collections;
using Pathfinding;
using Unity.Netcode;
using UnityEngine;

public class LurkerRoamState : LurkerBaseState
{
    private FollowerEntity agent;
    private LurkerAnimationManager anim;
    private float openingDoorRange = 1f;
    public override void EnterState(LurkerMonsterScript lurker)
    {
        agent = lurker.agent;
        anim = lurker.animationManager;
        agent.maxSpeed = lurker.roamSpeed;
        lurker.currentTarget = null;
        AudioManager.Instance.Stop3DSoundServerRpc(AudioManager.Instance.Get3DSoundFromList(lurker.lurkerPreChaseSFX));
        NetworkObject lurkerNetworkObject = lurker.GetComponent<NetworkObject>();
        if (lurkerNetworkObject != null && lurkerNetworkObject.IsSpawned)
        {
            AudioManager.Instance.Play3DSoundServerRpc(
                AudioManager.Instance.Get3DSoundFromList(lurker.lurkerBreathingSFX),
                lurker.transform.position,
                false,
                1f,
                1f,
                30f,
                true,
                lurkerNetworkObject
            );
        }
        else
        {
            Debug.LogWarning("Tried to play 3D sound with unspawned NetworkObject!");
        }
    }

    public override void UpdateState(LurkerMonsterScript lurker)
    {
        agent.stopDistance = lurker.defaultStoppingDistance;
        agent.maxSpeed = lurker.roamSpeed;
        agent.canMove = true;

        SetPathfindingConstraints(true, 1 << 2);

        //Set animations
        anim.PlayWalkAnimation();
        if (agent.velocity.magnitude > 0.1f && agent.canMove && !agent.reachedEndOfPath && agent.hasPath)
        {
            lurker.HandleNormalFootStepSFX();
        }

        //open door
        if (lurker.houseMapGenerator.GetRoomFromPosition(lurker.transform.position) != null)
            CheckForDoor(lurker);

        //Search for random point to roam to
        if (!agent.pathPending && (agent.reachedEndOfPath || !agent.hasPath))
        {
            NNInfo sample = AstarPath.active.graphs[0].RandomPointOnSurface(NNConstraint.Walkable);
            agent.destination = sample.position;
            agent.SearchPath();
        }

        TrySwitchToStalkState(lurker);
    }


    private void SetPathfindingConstraints(bool constrainTags, int tagMask)
    {
        if (AstarPath.active.GetNearest(agent.position).node.Tag != 1)
        {
            NNConstraint constraint = NNConstraint.Walkable;
            constraint.constrainTags = constrainTags;
            constraint.tags = tagMask; //only allow constraint to pick nodes with this tag
            agent.pathfindingSettings.traversableTags = tagMask; //only allow agent to move through nodes with this tag
        }
    }

    private void TrySwitchToStalkState(LurkerMonsterScript lurker)
    {
        //Random chance to switch to stalk state. checks every second
        float chance = Random.value;
        if (chance * 100 <= lurker.chanceToStalkPlayer * Time.deltaTime && lurker.canStalk)
        {
            lurker.SetRandomPlayerAsTarget();
            if (lurker.currentTarget != null)
            {

                lurker.SwitchState(LurkerState.Stalking);
            }

        }
    }

    private void CheckForDoor(LurkerMonsterScript lurker)
    {
        Collider[] colliders = Physics.OverlapSphere(lurker.transform.position, openingDoorRange, lurker.doorLayer);
        foreach (Collider collider in colliders)
        {
            Door door = collider.GetComponent<Door>();
            if (door != null)
            {
                door.OpenDoor(lurker.transform.position);
            }
        }

    }


}
