using UnityEngine;
using Unity.Netcode;
using UnityEngine.Animations.Rigging;
using UnityEditor.Animations;
public class LurkerAnimationManager : NetworkBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private LurkerMonsterScript lurkerScript;
    [SerializeField] private MultiAimConstraint multiAimConstraint;
    [SerializeField] private Transform sourceTarget;
    private int baseLayer, headLayer, rightArmLayer, leftArmLayer;
    public NetworkVariable<float> currentAnimSpeed = new NetworkVariable<float>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private int emptyStateHash;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

    }
    void Start()
    {
        baseLayer = animator.GetLayerIndex("Base Layer");
        headLayer = animator.GetLayerIndex("Head");
        rightArmLayer = animator.GetLayerIndex("Right Arm");
        leftArmLayer = animator.GetLayerIndex("Left Arm");
        emptyStateHash = Animator.StringToHash("Empty");
    }
    
    void OnEnable()
    {
        currentAnimSpeed.OnValueChanged += OnAnimSpeedChange;
    }

    void OnDisable()
    {
        currentAnimSpeed.OnValueChanged -= OnAnimSpeedChange;
    }
    
    private void OnAnimSpeedChange(float previousSpeed, float newSpeed)
    {
        if(!IsServer)
        {
            animator.speed = newSpeed;
            //Debug.Log("changed speed: "  + newSpeed);
        }
    }

    void Update()
    {
        if(IsServer)
        {
            SetAnimationSpeed();
		    animator.speed = currentAnimSpeed.Value;
        }
        
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
    
    private void SetAnimationSpeed()
	{
		if (lurkerScript.networkState.Value == LurkerState.Chasing)
		{
			currentAnimSpeed.Value = lurkerScript.inLight.Value ? 0.75f : 2;
		}else
		{
		    // Default animation speed
			currentAnimSpeed.Value = 1;
		}
		
	}
    
    private void PlayRandomTwitch()
    {
        float rand = Random.value;
        if(rand < 0.5f)
        {
            animator.SetTrigger("AggressiveTwitch");
        }else
        {
            animator.SetTrigger("SlightTwitch");
        }
    }
    public void PlayHeadTwitches()
    {
        animator.SetLayerWeight(headLayer, 1);
        PlayRandomTwitch();
    }
    
    public void PlayRightArmTwitch()
    {
        animator.SetLayerWeight(rightArmLayer, 1);
        PlayRandomTwitch();
    }
    
    public void PlayLeftArmTwitch()
    {
        animator.SetLayerWeight(leftArmLayer, 1);
        PlayRandomTwitch();
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
