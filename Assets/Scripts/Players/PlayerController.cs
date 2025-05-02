using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using Unity.Netcode.Components;
using System.Collections;
using UnityEngine.TextCore.Text;

public enum PlayerState
{
	Walking,
	Running,
	Crouching,
	Hiding,
	MovementLocked
}
public class PlayerController : NetworkBehaviour, ILurkerJumpScare
{
	public PlayerState currentState { get; private set; }
	public NetworkVariable<Quaternion> currentRotation = new NetworkVariable<Quaternion>(Quaternion.identity, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

	[Header("Initialize")]
	[SerializeField] private CharacterController characterController;
	[SerializeField] private PlayerScriptable playerScriptableObj;
	[SerializeField] private SkinnedMeshRenderer meshRenderer;
	[SerializeField] private CapsuleCollider playerCollider;

	[Header("Camera")]
	[SerializeField] private Transform camFollowPivot;
	private float targetCamHeight;
	private float currentCamHeight;
	private float cameraSmoothDampVelocity = 0f;


	[Header("Movement")]
	private float baseMoveSpeed;
	public float moveSpeed;
	public Vector2 inputDir;
	private Vector3 smoothedDirection;
	private Vector3 playerVelocity;
	private Vector3 smoothDampVelocity = Vector3.zero;
	[SerializeField] private float moveSmoothTime;
	[SerializeField] private float gravity = -15f;
	private bool canMove;

	[Header("Sprinting")]
	[SerializeField] private float addedSprintSpeed;
	private bool enabledSprinting;

	[Header("Crouching")]
	[SerializeField] private float standHeight;
	[SerializeField] private float crouchHeight;
	[SerializeField] private float crouchSpeedMultiplier;
	[SerializeField] private float crouchSmoothTime;
	private bool enabledCrouching;
	[Header("Jump")]
	[SerializeField] private float jumpHeight;
	[SerializeField] private float airResistanceMultiplier;
	[Header("Ground Check")]
	public Transform groundCheckTransform;
	[SerializeField] private LayerMask groundCheckLayer;
	public bool isGrounded;
	public float floorPosition;
	[Header("Animation")]
	[SerializeField] private Animator animator;
	//player effects
	private Coroutine speedUpCoroutine;
	private int currentSpeedBoost = 0;

	void Awake()
	{
		//initializing player character controller
		characterController = GetComponent<CharacterController>();
	}

	public override void OnNetworkSpawn()
	{
		if (IsOwner)
		{
			// characterController.enabled = false;
			// Debug.Log(PlayerNetworkManager.Instance.DetermineSpawnPosition(PlayerNetworkManager.Instance.spawnPosition.Value));
			// transform.position = PlayerNetworkManager.Instance.DetermineSpawnPosition(PlayerNetworkManager.Instance.spawnPosition.Value);
			characterController.enabled = true;
		}
		else
		{
			gameObject.GetComponent<CharacterController>().enabled = false;
			gameObject.GetComponent<PlayerInput>().enabled = false;
		}
		if (IsServer)
		{
			PlayerNetworkManager.Instance.RegisterPlayerClientRpc(GetComponent<NetworkObject>());
		}
	}

	void Start()
	{

		if (IsOwner)
		{
			//meshRenderer.enabled = false;
			canMove = true;
			baseMoveSpeed = playerScriptableObj.baseMovementSpeed;
			moveSpeed = baseMoveSpeed; //setting movespeed to default base speed
			targetCamHeight = standHeight;

			//setting booleans
			enabledCrouching = false;
			enabledSprinting = false;


		}


	}

	public void OnMove(InputAction.CallbackContext ctx)
	{
		if (!canMove) return;
		inputDir = ctx.ReadValue<Vector2>(); //getting the player's input values. example: input A returns (-1, 0)

	}

	public void OnCrouch(InputAction.CallbackContext ctx)
	{
		//later probably add a way to switch between toggle to crouch and hold to crouch (for now its toggle to crouch)
		if (ctx.performed && !enabledCrouching && isGrounded && canMove)
		{
			targetCamHeight = crouchHeight;
			enabledCrouching = true;
		}
		else if (ctx.performed && enabledCrouching && isGrounded)
		{
			targetCamHeight = standHeight;
			enabledCrouching = false;
		}
	}

	public void OnSprint(InputAction.CallbackContext ctx)
	{
		//later probably add a limit to the spring and/or stamina bar
		if (ctx.performed && !enabledSprinting && isGrounded)
			enabledSprinting = true;

		if (ctx.canceled && enabledSprinting)
			enabledSprinting = false;

	}

	public void OnJump(InputAction.CallbackContext ctx)
	{
		if (isGrounded && ctx.performed && canMove)
		{
			playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
		}
	}

	private void CrouchFunctionality()
	{
		currentCamHeight = Mathf.SmoothDamp(currentCamHeight, targetCamHeight, ref cameraSmoothDampVelocity, crouchSmoothTime * Time.deltaTime);
		Vector3 newCamPosition = camFollowPivot.localPosition;
		newCamPosition.y = currentCamHeight;
		camFollowPivot.localPosition = newCamPosition;
	}

	public Vector3 GetPlayerGroundedPosition()
	{
		//adjusted player position to account for player jumping
		return new Vector3(transform.position.x, floorPosition, transform.position.z);
	}

	public Vector3 GetPlayerCameraPosition()
	{
		return camFollowPivot.position;
	}
	private void HandleAnimations()
	{
		Vector2 rawInput = inputDir;

		// Normalize but keep diagonals softer
		Vector2 normalizedInput = rawInput.normalized;

		float forwardAmount = normalizedInput.y;
		float sidewaysAmount = normalizedInput.x;

		if (rawInput.magnitude > 0.1f)
		{
			// Forward/back speed scaling
			if (currentState == PlayerState.Running)
				forwardAmount *= 1f;
			else if (currentState == PlayerState.Walking)
				forwardAmount *= 0.5f;
			else if (currentState == PlayerState.Crouching)
				forwardAmount *= 0.25f;

			// Strafe scaling (keep -0.5 to 0.5 range for sideways)
			sidewaysAmount = Mathf.Clamp(sidewaysAmount, -0.5f, 0.5f);
		}
		else
		{
			forwardAmount = 0f;
			sidewaysAmount = 0f;
		}

		// Smooth animation parameters
		float lerpSpeed = 10f;
		float moveX = Mathf.Lerp(animator.GetFloat("moveX"), sidewaysAmount, Time.deltaTime * lerpSpeed);
		float moveY = Mathf.Lerp(animator.GetFloat("moveY"), forwardAmount, Time.deltaTime * lerpSpeed);

		if (Mathf.Abs(moveX) < 0.01f) moveX = 0f;
		if (Mathf.Abs(moveY) < 0.01f) moveY = 0f;

		animator.SetFloat("moveX", moveX);
		animator.SetFloat("moveY", moveY);

		float moveMagnitude = new Vector2(moveX, moveY).magnitude;
		animator.SetFloat("moveMagnitude", moveMagnitude);

		animator.SetFloat("crouchBlend", currentState == PlayerState.Crouching ? 1f : 0f);
	}



	void Update()
	{
		if (IsOwner && characterController.enabled == true)
		{
			if (!canMove)
			{
				currentState = PlayerState.MovementLocked;
			}

			//checking to see if sphere collider is touching the ground to determine if player is grounded or not
			isGrounded = Physics.CheckSphere(groundCheckTransform.position, 0.25f, groundCheckLayer);

			Vector3 moveDir = Camera.main.transform.right * inputDir.x + Camera.main.transform.forward * inputDir.y;
			moveDir.y = 0;
			Vector3 targetDirection = moveDir.normalized; //normalizing movement direction to prevent diagonal direction from moving faster	
			smoothedDirection = Vector3.SmoothDamp(smoothedDirection, targetDirection, ref smoothDampVelocity, moveSmoothTime);
			HandleAnimations();

			// Apply gravity
			if (!isGrounded)
				playerVelocity.y += gravity * Time.deltaTime;

			characterController.Move(playerVelocity * Time.deltaTime);

			if (isGrounded)
				characterController.Move(smoothedDirection * moveSpeed * Time.deltaTime);
			else
				characterController.Move(smoothedDirection * moveSpeed * airResistanceMultiplier * Time.deltaTime);

			CrouchFunctionality();

			switch (currentState)
			{
				case PlayerState.Walking:
					moveSpeed = baseMoveSpeed + currentSpeedBoost;
					if (enabledSprinting)
						currentState = PlayerState.Running;
					else if (enabledCrouching)
					{
						currentState = PlayerState.Crouching;
					}
					break;
				case PlayerState.Running:
					moveSpeed = baseMoveSpeed + addedSprintSpeed + currentSpeedBoost;
					if (enabledCrouching)
					{
						enabledSprinting = false;
						targetCamHeight = crouchHeight;
						currentState = PlayerState.Crouching;
					}
					else if (!enabledSprinting)
						currentState = PlayerState.Walking;
					break;
				case PlayerState.Crouching:

					moveSpeed = baseMoveSpeed * crouchSpeedMultiplier + currentSpeedBoost;
					if (enabledSprinting)
					{
						enabledCrouching = false;
						targetCamHeight = standHeight;
						currentState = PlayerState.Running;
					}
					else if (!enabledCrouching)
						currentState = PlayerState.Walking;
					break;
				case PlayerState.Hiding:
					break;
				case PlayerState.MovementLocked:
					moveSpeed = 0;
					inputDir = Vector3.zero;
					targetCamHeight = standHeight;
					enabledCrouching = false;
					if (canMove)
					{
						if (enabledSprinting)
							currentState = PlayerState.Running;
						else
							currentState = PlayerState.Walking;
					}

					break;
				default:
					break;
			}


		}

	}

	void FixedUpdate()
	{

		if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, Mathf.Infinity, groundCheckLayer))
		{
			floorPosition = hit.collider.transform.position.y;
		}
	}

	public void ApplyAnimationLock(float animationTime)
	{
		Debug.Log("called animation lock");
		StartCoroutine(AnimationLocked(animationTime));
	}

	private IEnumerator AnimationLocked(float lockTime)
	{
		canMove = false;
		yield return new WaitForSeconds(lockTime);
		canMove = true;
	}

	public void SpeedUp(float duration, int speedBoost)
	{
		if (speedUpCoroutine != null)
		{
			moveSpeed -= currentSpeedBoost;
			StopCoroutine(speedUpCoroutine);
		}
		currentSpeedBoost = speedBoost;
		GetComponent<PlayerCamera>().currentFOVSpeedBoost = 25f;

		speedUpCoroutine = StartCoroutine(StartSpeedUp(duration));
	}

	private IEnumerator StartSpeedUp(float duration)
	{
		yield return new WaitForSeconds(duration);
		GetComponent<PlayerCamera>().currentFOVSpeedBoost = 0f;
		currentSpeedBoost = 0;
		speedUpCoroutine = null;
	}


}
