using UnityEngine;
using FishNet.Object;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using System;



public enum PlayerState
{
	Walking,
	Running,
	Crouching,
	Hiding
}
public class PlayerController : NetworkBehaviour
{
 	[SerializeField] private PlayerState currentState;

	[Header("Initialize")]
	[SerializeField] private CharacterController characterController;
	[SerializeField] private PlayerScriptable playerScriptableObj;
	
	[Header("Camera")]
	[SerializeField] private Transform headPosition;
	private Vector3 headOriginalPosition;
	private float targetCamHeight;
	private float currentCamHeight;
	private float cameraSmoothDampVelocity = 0f;
	private CinemachineCamera cmCam;

	[Header("Head Bob")]
	private CinemachineBasicMultiChannelPerlin camNoiseChannel;
	[SerializeField] private bool enableHeadBob = true;
	[SerializeField] private float idleBobAmplitude = 0.2f, idleBobFrequency = 0.4f;
	[SerializeField, Range(0, 2f)] private float bobAmount = 0.02f; //amplitude
	[SerializeField, Range(0, 30)] private float frequency = 15f;
	[SerializeField, Range(10f, 100f)] private float smoothtime = 30.0f;
	[SerializeField] private float sprintBobMultiplier = 1.5f;
	
	[Header("Movement")]
	private float baseMoveSpeed;
	private float moveSpeed;
	private Vector2 inputDir; 
	private Vector3 smoothedDirection;
	private Vector3 playerVelocity;
	private Vector3 smoothDampVelocity = Vector3.zero;
	[SerializeField] private float moveSmoothTime = 0.1f;
	[SerializeField] private float gravity = -15f;

	[Header("Sprinting")]
	[SerializeField] private float addedSprintSpeed;
	public static event Action onSprint;
	private bool enabledSprinting;

	[Header("Crouching")]
	[SerializeField] private float standHeight;
	[SerializeField] private float crouchHeight = 0.6f;
	[SerializeField] private float crouchSpeedMultiplier;
	[SerializeField] private float crouchSmoothTime = 5f;
	private bool enabledCrouching;
	[Header("Jump")]
	[SerializeField] private float jumpHeight;
	[SerializeField] private float airResistanceMultiplier;
	[Header("Ground Check")]
	[SerializeField] private Transform groundCheckTransform;
	[SerializeField] private LayerMask groundCheckLayer;
	private bool isGrounded;
	
	public override void OnStartClient()
	{
		base.OnStartClient();
		if(base.IsOwner)
		{
			cmCam = FindFirstObjectByType<CinemachineCamera>();
			camNoiseChannel = cmCam.GetComponentInChildren<CinemachineBasicMultiChannelPerlin>();
			
			if(cmCam != null)
			{
				cmCam.Follow = headPosition;
			}
		}
		else
		{
			gameObject.GetComponent<PlayerInput>().enabled = false;
			gameObject.GetComponent<PlayerController>().enabled = false;
			
		}
	}

	void Start()
	{
		Cursor.lockState = CursorLockMode.Locked;
		baseMoveSpeed = playerScriptableObj.baseMovementSpeed;
		moveSpeed = baseMoveSpeed; //setting movespeed to default base speed
		targetCamHeight = standHeight;
		headOriginalPosition = headPosition.localPosition;
		
		//setting booleans
		enabledCrouching = false;
		enabledSprinting = false;


		//initializing player character controller
		characterController = GetComponent<CharacterController>();
	}


	public void OnMove(InputAction.CallbackContext ctx)
	{
		inputDir = ctx.ReadValue<Vector2>(); //getting the player's input values. example: input A returns (-1, 0)
	}
	
	public void OnCrouch(InputAction.CallbackContext ctx)
	{
		//later probably add a way to switch between toggle to crouch and hold to crouch (for now its toggle to crouch)
		if(ctx.performed && !enabledCrouching && isGrounded)
		{
			targetCamHeight = crouchHeight;
			enabledCrouching = true;
		}else if(ctx.performed && enabledCrouching && isGrounded)
		{
			targetCamHeight = standHeight;
			enabledCrouching = false;   
		}
	}

	public void OnSprint(InputAction.CallbackContext ctx)
	{
		//later probably add a limit to the spring and/or stamina bar
		if(ctx.performed && !enabledSprinting && isGrounded)
			enabledSprinting = true;
		
		if(ctx.canceled && enabledSprinting)
			enabledSprinting = false;
		
	}

	public void OnJump(InputAction.CallbackContext ctx)
	{
		if(isGrounded && ctx.performed)
		{
			playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
		}
	}
	
	private void CrouchFunctionality()
	{
		currentCamHeight = Mathf.SmoothDamp(currentCamHeight, targetCamHeight, ref cameraSmoothDampVelocity, crouchSmoothTime * Time.deltaTime);
		Vector3 newCamPosition = headPosition.localPosition;
		newCamPosition.y = currentCamHeight;
		headPosition.localPosition = newCamPosition;
	}
	
	private void StartHeadBob()
	{
		Vector3 pos = Vector3.zero;
		
		//change to make it based on velocity with it being clamped
		if(enabledSprinting)
		{
			pos.y += Mathf.Lerp(pos.y, Mathf.Sin(Time.time * frequency * sprintBobMultiplier) * bobAmount * 6f * sprintBobMultiplier, smoothtime * Time.deltaTime);
			pos.x += Mathf.Lerp(pos.x, Mathf.Cos(Time.time * frequency /2f * sprintBobMultiplier) * bobAmount * 0.7f * sprintBobMultiplier, smoothtime * Time.deltaTime);
		}
		else
		{
			pos.y += Mathf.Lerp(pos.y, Mathf.Sin(Time.time * frequency) * bobAmount * 6f, smoothtime * Time.deltaTime);
			pos.x += Mathf.Lerp(pos.x, Mathf.Cos(Time.time * frequency /2f) * bobAmount * 0.7f, smoothtime * Time.deltaTime);
		}
		
		headPosition.localPosition += pos;
	}
	
	private void StopHeadBob()
	{
		if(headPosition.localPosition == headOriginalPosition) return;
		headPosition.localPosition = Vector3.Lerp(headPosition.localPosition, headOriginalPosition, 1 * Time.deltaTime);
	}
	
	private void StopHeadSway()
	{
		if(camNoiseChannel.AmplitudeGain == 0 || camNoiseChannel.FrequencyGain == 0) return;
		camNoiseChannel.AmplitudeGain = Mathf.Lerp(camNoiseChannel.AmplitudeGain, 0, 1 * Time.deltaTime);
		camNoiseChannel.FrequencyGain = Mathf.Lerp(camNoiseChannel.FrequencyGain, 0, 1 * Time.deltaTime);
	}
	
	
	private void HeadBobbing()
	{
		StopHeadBob();
		if(camNoiseChannel != null)
		{		
			if(inputDir == Vector2.zero || !isGrounded)
			{
				
				camNoiseChannel.AmplitudeGain = idleBobAmplitude;
				camNoiseChannel.FrequencyGain = idleBobFrequency;

			}
			else
			{
				StartHeadBob();
				StopHeadSway();
			}
		}

	}
	
	void Update()
	{
		//checking to see if sphere collider is touching the ground to determine if player is grounded or not
		isGrounded = Physics.CheckSphere(groundCheckTransform.position, 0.25f, groundCheckLayer); 
		Vector3 moveDir = Camera.main.transform.right * inputDir.x + Camera.main.transform.forward * inputDir.y; 
		moveDir.y = 0;
		Vector3 targetDirection = moveDir.normalized; //normalizing movement direction to prevent diagonal direction from moving faster	
		smoothedDirection = Vector3.SmoothDamp(smoothedDirection, targetDirection, ref smoothDampVelocity, moveSmoothTime);
		// Apply gravity
		if(!isGrounded)
			playerVelocity.y += gravity * Time.deltaTime;
			
		characterController.Move(playerVelocity * Time.deltaTime);
		
		if(isGrounded)
			characterController.Move(smoothedDirection * moveSpeed * Time.deltaTime);	
		else
			characterController.Move(smoothedDirection * moveSpeed * airResistanceMultiplier * Time.deltaTime);	
			
		CrouchFunctionality();
		HeadBobbing();
		
		switch(currentState)
		{
			case PlayerState.Walking:
				moveSpeed = baseMoveSpeed;
				if(enabledSprinting)
					currentState = PlayerState.Running;
				else if(enabledCrouching)
				{
					currentState = PlayerState.Crouching;
				}
				break;
			case PlayerState.Running:
				moveSpeed = baseMoveSpeed + addedSprintSpeed;
				if(enabledCrouching)
				{
					enabledSprinting = false;
					targetCamHeight = crouchHeight;
					currentState = PlayerState.Crouching;
				}
				else if(!enabledSprinting)             
					currentState = PlayerState.Walking;
				break;
			case PlayerState.Crouching:
					
				moveSpeed = baseMoveSpeed * crouchSpeedMultiplier;
				if(enabledSprinting)
				{
					enabledCrouching = false;
					targetCamHeight = standHeight;
					currentState = PlayerState.Running;
				}
				else if(!enabledCrouching)
					currentState = PlayerState.Walking;
				break;
			case PlayerState.Hiding:
				break;
			default:
				break;
		}
		
	}


}
