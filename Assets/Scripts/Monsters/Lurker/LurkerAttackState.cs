using UnityEngine;
using System.Collections;

using Unity.Netcode;
public class LurkerAttackState : LurkerBaseState
{
    private Animator animator;

    public override void EnterState(LurkerMonsterScript lurker)
    {
        animator = lurker.anim;
   
        lurker.StartCoroutine(AttackPlayer(lurker));
    }

    public override void UpdateState(LurkerMonsterScript lurker)
    {

    }

    private IEnumerator AttackPlayer(LurkerMonsterScript lurker)
    {
        lurker.animationManager.PlayAttackAnimation();
        // Wait for the animation to actually start playing before checking its length
        yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(lurker.animationManager.attackLayer).normalizedTime >= 0.1f);
        AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(lurker.animationManager.attackLayer);
        float animationTime = clipInfo.Length > 0 ? clipInfo[0].clip.length : 1.0f; // Default to 1s if not found
        lurker.AnimationLockClientRpc(lurker.currentTarget.GetComponent<NetworkObject>(), animationTime);
        lurker.SmoothRotateToLurkerClientRpc(lurker.currentTarget.GetComponent<NetworkObject>(), 0.5f);
        yield return new WaitForSeconds(animationTime);

        PlayerHealth playerHealth = lurker.currentTarget.GetComponent<PlayerHealth>();
        playerHealth.TakeDamageServerRpc(lurker.lurkerScriptableObj.damage);
        lurker.StartStalkCooldown();
        lurker.StartAttackCooldown();
        lurker.SwitchState(LurkerState.Roaming);
    }

    
}
