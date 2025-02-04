
using System;
using System.Collections;

using Unity.Cinemachine;

using UnityEngine;
using UnityEngine.InputSystem;

/*
public enum PlayerState
{
	Walking,
	Running,
	Crouching,
	Hiding
}
*/
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovementRB : MonoBehaviour, IPlayerCollision
{
	private PlayerState currentState;

	[Header("Initialize")]
	[SerializeField] private Rigidbody playerRb;
	[SerializeField] private PlayerScriptable playerScriptableObj;
	
	[Header("Camera")]
	[SerializeField] private Transform headPosition;
	private float targetCamHeight;
	private float currentCamHeight;
	private float velocity = 0f;
	private CinemachineCamera cmCam;

	[Header("Movement")]
	private float baseMoveSpeed;
	private float moveSpeed;
	[SerializeField] private float groundDrag; //adding drag to movement so player is not slippery on the ground
	private Vector2 inputDir; 
	private Vector3 moveDirection;
	[SerializeField] private float velocityReductionFactor = 0.5f;

	[Header("Sprinting")]
	[SerializeField] private float addedSprintSpeed;
	private bool enabledSprinting;

	[Header("Crouching")]
	[SerializeField] private float standHeight;
	[SerializeField] private float crouchHeight = 0.6f;
	[SerializeField] private float crouchSpeedMultiplier;
	[SerializeField] private float crouchSmoothTime = 5f;
	private bool enabledCrouching;
	[Header("Jump")]
	[SerializeField] private float jumpForce;
	[SerializeField] private float fallForce;
	[SerializeField] private float jumpCooldown;
	[SerializeField] private float airMultiplier; //adds some air resistance while jumping
	private bool canJump;

	[Header("Ground Check")]
	[SerializeField] private Transform groundCheckTransform;
	[SerializeField] private LayerMask groundCheckLayer;
	private bool isGrounded;

	void Start()
	{
		Cursor.lockState = CursorLockMode.Locked;
		baseMoveSpeed = playerScriptableObj.baseMovementSpeed;
		moveSpeed = baseMoveSpeed; //setting movespeed to default base speed
		targetCamHeight = standHeight;

		//setting booleans
		enabledCrouching = false;
		enabledSprinting = false;
		canJump = true;

		//initializing player rigidbody component
		playerRb = gameObject.GetComponent<Rigidbody>();

	}

	public void OnCollisionStay(Collision collision)
	{
		
		IPlayerCollision otherPlayer = collision.gameObject.GetComponent<IPlayerCollision>();
		if(otherPlayer != null)
		{
			//reduce velocity when colliding with other players so players cant push each other too much
			Vector3 directionToPlayer = (transform.position - collision.gameObject.transform.position).normalized;
			//if dot product is closer to 1, then that means these two vectors are in the same direction, if dot product is 0 then these two vectors are forming a 90 deg angle
			float dotProduct = Vector3.Dot(playerRb.linearVelocity.normalized, directionToPlayer); 
			
			if(dotProduct > 0.5)
			{	
				Vector3 playerVelocity = new Vector3(playerRb.linearVelocity.x, 0f, playerRb.linearVelocity.z);
				playerVelocity /= velocityReductionFactor;
				playerRb.linearVelocity = new Vector3(playerVelocity.x, playerRb.linearVelocity.y, playerVelocity.z);
			
			}

			
		}
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
		if(isGrounded && ctx.performed && canJump)
		{
			playerRb.linearVelocity = new Vector3(playerRb.linearVelocity.x, 0f, playerRb.linearVelocity.z);
			playerRb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
			StartCoroutine(ApplyJumpCooldown());
		}
	}

	private IEnumerator ApplyJumpCooldown()
	{
		canJump = false;
		yield return new WaitForSeconds(jumpCooldown);
		canJump = true;
	}
	
	private void CalculateMovementDirection()
	{
		Vector3 moveDir = Camera.main.transform.right * inputDir.x + Camera.main.transform.forward * inputDir.y; 
		moveDir.y = 0;
		moveDirection = moveDir.normalized; //normalizing movement direction to prevent diagonal direction from moving faster		
	}
	
	private void ApplyCrouching()
	{
		currentCamHeight = Mathf.SmoothDamp(currentCamHeight, targetCamHeight, ref velocity, crouchSmoothTime * Time.deltaTime);
		Vector3 newCamPosition = headPosition.localPosition;
		newCamPosition.y = currentCamHeight;
		headPosition.localPosition = newCamPosition;
	}
	
	void Update()
	{
		//checking to see if sphere collider is touching the ground to determine if player is grounded or not
		isGrounded = Physics.CheckSphere(groundCheckTransform.position, 0.25f, groundCheckLayer); 
		CalculateMovementDirection();
		LimitSpeed();
		ApplyCrouching();
		

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

	private void LimitSpeed()
	{
		//limiting the speed so that when the player moves diagonally, their velocity remains the same and does not speed up.
		Vector3 horizontalVelocity = new Vector3(playerRb.linearVelocity.x, 0f, playerRb.linearVelocity.z);
		if(horizontalVelocity.magnitude > moveSpeed)
		{
			Vector3 maxVelocity = playerRb.linearVelocity.normalized * moveSpeed;
			playerRb.linearVelocity = new Vector3(maxVelocity.x, playerRb.linearVelocity.y, maxVelocity.z);
		}
	}
	
	private void FixedUpdate()
	{
		playerRb.linearDamping = isGrounded ? groundDrag : 0; //if player is on the ground, we apply the ground drag and if in the air we need 0 drag resistance
		if(isGrounded)
		{
			playerRb.AddForce(moveDirection * moveSpeed * 10f, ForceMode.Force);
		}
		else
		{
			playerRb.AddForce(moveDirection * moveSpeed * 10f * airMultiplier, ForceMode.Force); //apply air resistance multiplier to slow down movement in the air
			if(playerRb.linearVelocity.y < 0)
			{
				playerRb.AddForce(Vector3.down * fallForce * 10f, ForceMode.Acceleration); //apply some downward force when velocity is less than 0 meaning player is falling
			}
		}

	}


}
