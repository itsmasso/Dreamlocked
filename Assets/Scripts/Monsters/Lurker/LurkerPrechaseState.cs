using UnityEngine;

public class LurkerPrechaseState : LurkerBaseState
{
    private LurkerAnimationManager anim;
    private float prechaseTimer;
    public override void EnterState(LurkerMonsterScript lurker)
    {
        anim = lurker.animationManager;
        prechaseTimer = 0;
    }

    public override void UpdateState(LurkerMonsterScript lurker)
    {
		lurker.agent.canMove = false;
		
		//Set animations
		anim.PlayPrechaseAnimation();
		
		
		prechaseTimer += Time.deltaTime;
		if(prechaseTimer > lurker.pauseBeforeChasingDuration)
		{
			lurker.SwitchState(LurkerState.Chasing);
		}
    }
}
