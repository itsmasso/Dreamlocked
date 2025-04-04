using UnityEngine;
using System.Collections;
using Unity.Cinemachine;
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
        RotatePlayer(lurker.currentTarget, lurker);
    }
    
    private IEnumerator AttackPlayer(LurkerMonsterScript lurker)
    {
        lurker.animationManager.PlayAttackAnimation();
        // Wait for the animation to actually start playing before checking its length
        yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(lurker.animationManager.attackLayer).normalizedTime >= 0.1f);
        AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(lurker.animationManager.attackLayer);
        float animationTime = clipInfo.Length > 0 ? clipInfo[0].clip.length : 1.0f; // Default to 1s if not found
        ILurkerJumpScare[] animationLocks = lurker.currentTarget.GetComponents<ILurkerJumpScare>();
        foreach(var animationLock in animationLocks)
            animationLock.ApplyAnimationLock(animationTime);
        
        yield return new WaitForSeconds(animationTime);

        PlayerHealth playerHealth = lurker.currentTarget.GetComponent<PlayerHealth>();
        playerHealth.TakeDamageServerRpc(lurker.lurkerScriptableObj.damage);
        lurker.StartStalkCooldown();
        lurker.StartAttackCooldown();
        lurker.SwitchState(LurkerState.Roaming);
    }
    
    private void RotatePlayer(Transform currentTarget, LurkerMonsterScript lurker)
    {
        if(currentTarget != null)
        {
            //body rotation
            Vector3 bodyDirection = lurker.transform.position - currentTarget.transform.position;
            bodyDirection.y = 0; 
            currentTarget.rotation = Quaternion.Slerp(currentTarget.rotation, Quaternion.LookRotation(bodyDirection), 8 * Time.deltaTime);

            //camera rotation
            CinemachinePanTilt panTilt = currentTarget.GetComponent<PlayerCamera>().GetPlayerCamRotation();
            if(panTilt != null)
            {
                Vector3 headDirection = lurker.headTransform.position - currentTarget.transform.position;
                Quaternion targetRotation = Quaternion.LookRotation(headDirection, headDirection);

                panTilt.PanAxis.Value = Mathf.LerpAngle(panTilt.PanAxis.Value, targetRotation.eulerAngles.y , 8 * Time.deltaTime);
                panTilt.TiltAxis.Value = Mathf.LerpAngle(panTilt.TiltAxis.Value, targetRotation.eulerAngles.x, 8 * Time.deltaTime);
            }
                
        }
    }
}
