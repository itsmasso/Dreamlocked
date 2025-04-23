using UnityEngine;
using Unity.Netcode;
public class MannequinAnimationManager : NetworkBehaviour
{
    [SerializeField] private Animator animator;
    private int baseLayer, attackLayer;
    private int randomInt;
    private bool setIntAlready;
    void Start()
    {
        baseLayer = animator.GetLayerIndex("Base Layer");
        attackLayer = animator.GetLayerIndex("Attack");
        setIntAlready = false;
    }

    public void PlayIdleAnimation()
    {
        if(!setIntAlready)
        {
            randomInt = Random.Range(1, 4);
            setIntAlready = true;
        }
        animator.SetBool("IsActive", false);
        animator.SetInteger("IdleType", randomInt);
    }
    public void PlayWalkAnimation()
    {
        animator.SetBool("IsActive", true);
    }

    public void PlayAttackAnimation()
    {
        animator.SetLayerWeight(attackLayer, 1);
        animator.SetTrigger("Attack");
        setIntAlready = false;
        
    }
    
    public void SetAnimationSpeed(float speed)
    {
        animator.speed = speed;
    }
}
