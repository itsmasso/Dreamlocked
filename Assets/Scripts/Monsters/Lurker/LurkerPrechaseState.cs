using UnityEngine;
using Unity.Netcode;
public class LurkerPrechaseState : LurkerBaseState
{
    private LurkerAnimationManager anim;
    private float prechaseTimer;
    public override void EnterState(LurkerMonsterScript lurker)
    {
        anim = lurker.animationManager;
        prechaseTimer = 0;
        AudioManager.Instance.Stop3DSoundServerRpc(AudioManager.Instance.Get3DSoundFromList(lurker.lurkerBreathingSFX));
        AudioManager.Instance.Play3DSoundServerRpc(AudioManager.Instance.Get3DSoundFromList(lurker.lurkerPreChaseSFX), lurker.transform.position, false, 1f, 1f, 30f, true, lurker.GetComponent<NetworkObject>());
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
