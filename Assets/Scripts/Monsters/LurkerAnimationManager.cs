using UnityEngine;
using Unity.Netcode;
using UnityEngine.Animations.Rigging;
public class LurkerAnimationManager : NetworkBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private LurkerMonsterScript lurkerScript;
    [SerializeField] private MultiAimConstraint multiAimConstraint;
    [SerializeField] private Transform sourceTarget;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

    }
    void Start()
    {
 
    }

    void Update()
    {
        
    }

    void LateUpdate()
    {
        if(lurkerScript.currentTarget != null)
        {
            
            sourceTarget.position = lurkerScript.currentTarget.GetComponent<PlayerController>().GetPlayerCameraPosition();
            multiAimConstraint.weight = 1;
            
            
        }else
        {
            sourceTarget.position = transform.position;
            multiAimConstraint.weight = 0;
        }
   
    }
    public void PlayHeadTwitches()
    {
        animator.SetLayerWeight(1, 1);
        animator.SetLayerWeight(2, 0);
        animator.SetTrigger("Twitch");
    }
    
    public void PlayArmTwitches()
    {
        animator.SetLayerWeight(2, 1);
        animator.SetLayerWeight(1, 0);
        animator.SetTrigger("Twitch");
    }
    
    
    public void PlayIdleAnimation()
    {
        animator.SetBool("Idle", true);
        animator.SetBool("Walking", false);
        animator.SetBool("Running", false);
        animator.SetBool("Prechase", false);
    }
    
	public void PlayWalkAnimation()
	{
	    animator.SetBool("Idle", false);
        animator.SetBool("Walking", true);
        animator.SetBool("Running", false);
        animator.SetBool("Prechase", false);
	}
	
	public void PlayPrechaseAnimation()
	{
	    animator.SetBool("Idle", false);
        animator.SetBool("Walking", false);
        animator.SetBool("Running", false);
        animator.SetBool("Prechase", true);
	}
	
	public void PlayRunningAnimation()
	{
	    animator.SetBool("Idle", false);
        animator.SetBool("Walking", false);
        animator.SetBool("Running", true);
        animator.SetBool("Prechase", false);
	}
}
