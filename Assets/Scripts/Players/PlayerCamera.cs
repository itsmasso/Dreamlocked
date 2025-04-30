
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Cinemachine;
using UnityEngine.Rendering.Universal;
using System.Linq;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class PlayerCamera : NetworkBehaviour, ILurkerJumpScare
{

	private PlayerController playerController;
	private bool canMove;
	[Header("Camera Properties")]

	[SerializeField] private CinemachineCamera playerCam;

	[SerializeField] private CinemachinePanTilt cmCamPanTilt;
	[SerializeField] private CinemachineInputAxisController inputAxisController;
	[SerializeField] private Camera itemCamera;
	[SerializeField] private Transform camFollowPivot;
	private Transform mainCameraPosition;
	[SerializeField] private Transform itemPivot;
	private bool isDead;
	[Header("Zoom Settings")]
	[SerializeField] private Vector2 jumpScareFOV;
	[SerializeField] private Vector2 defaultFOV;
	[SerializeField] private float zoomSmoothTime;
	private CinemachineFollowZoom followZoom;

	[Header("Spectator Properties")]
	[SerializeField] private CinemachineCamera spectatorCam;
	public Transform spectatorTransform;
	private Transform currentPlayerToSpectate;

	[Header("Head Sway")]
	private CinemachineBasicMultiChannelPerlin camNoiseChannel;
	[SerializeField] private float idleBobAmplitude = 0.2f, idleBobFrequency = 0.4f;

	[Header("Head Bob")]
	[SerializeField] private float walkBobSpeed;
	[SerializeField] private float walkBobAmount; //how much the camera moves
	[SerializeField] private float sprintBobSpeed;
	[SerializeField] private float sprintBobAmount; //how much the camera moves
	[SerializeField] private float crouchBobSpeed;
	[SerializeField] private float crouchBobAmount; //how much the camera moves
	[SerializeField] private Vector3 originalCamPos;
	private float bobbingTimer;
	private float movingTimer;

	[Header("Player View")]
	[SerializeField] private LayerMask groundLayer;
	[SerializeField] private LayerMask enemyLayer;
	[SerializeField] private LayerMask obstacleLayer;
	[SerializeField] private LayerMask interactableMoveablesLayer;
	[SerializeField] private float peripheralAngle; //max angle that determines how wide the field of view extends around the player. If angle is 90 degrees, it means the view is limited to 45 to the left and right
	[Header("FOV Effects")]
	[SerializeField] private Vector2 sprintFOV;
	public float currentFOVSpeedBoost = 0f;
	[SerializeField] private float fovTransitionSpeed = 5f;

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
	}
	private void Start()
	{
		if (!IsOwner)
		{
			this.enabled = false;
			return;
		}

		// Find the PlayerCam Virtual Camera
		playerCam = GameObject.FindGameObjectWithTag("PlayerCamera")?.GetComponent<CinemachineCamera>();
		spectatorCam = GameObject.FindGameObjectWithTag("SpectatorCamera")?.GetComponent<CinemachineCamera>();

		if (playerCam == null) Debug.LogError("PlayerCamera not found!");
		if (spectatorCam == null) Debug.LogError("SpectatorCamera not found!");

		cmCamPanTilt = playerCam.GetComponent<CinemachinePanTilt>();
		playerCam.Follow = camFollowPivot;
		camNoiseChannel = playerCam.GetComponentInChildren<CinemachineBasicMultiChannelPerlin>();
		inputAxisController = playerCam.GetComponentInChildren<CinemachineInputAxisController>();
		followZoom = playerCam.GetComponentInChildren<CinemachineFollowZoom>();

		playerCam.enabled = true;
		itemCamera.enabled = true;
		spectatorCam.enabled = false;
		spectatorCam.gameObject.GetComponent<AudioListener>().enabled = false;

		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;

		playerController = GetComponent<PlayerController>();
		originalCamPos = camFollowPivot.localPosition;
		mainCameraPosition = Camera.main.transform;
		Camera.main.GetUniversalAdditionalCameraData().cameraStack.Add(itemCamera);

		PlayerHealth.onDeath += DisablePlayerCamera;
		isDead = false;
		canMove = true;
		defaultFOV = followZoom.FovRange;

		GameManager.Instance.onLobby += HandleLobbyLoading;
	}


	public void OnSpectateNext(InputAction.CallbackContext ctx)
	{
		if (ctx.performed && isDead)
		{
			currentPlayerToSpectate = PlayerNetworkManager.Instance.GetNextPlayerToSpectate().transform;
		}
	}
	public Transform GetSpectatorTransform()
	{
		return spectatorTransform;
	}

	public CinemachineCamera GetPlayerCam()
	{
		return playerCam;
	}
	private void DisablePlayerCamera()
	{
		playerCam.enabled = false;
		itemCamera.enabled = false;
		spectatorCam.enabled = true;
		spectatorCam.gameObject.GetComponent<AudioListener>().enabled = true;
		currentPlayerToSpectate = PlayerNetworkManager.Instance.GetNextPlayerToSpectate().transform;
		spectatorCam.Follow = camFollowPivot;
		isDead = true;
	}



	public CinemachinePanTilt GetPlayerCamRotation()
	{
		if (cmCamPanTilt != null)
			return cmCamPanTilt;
		else
			return null;
	}

	private void StartHeadBob()
	{
		bobbingTimer += Time.deltaTime * (playerController.currentState == PlayerState.Crouching ? crouchBobSpeed : playerController.currentState == PlayerState.Running ? sprintBobSpeed : walkBobSpeed);

		float bobAmount = playerController.currentState == PlayerState.Crouching ? crouchBobAmount : playerController.currentState == PlayerState.Running ? sprintBobAmount : walkBobAmount;
		camFollowPivot.localPosition = new Vector3(
			camFollowPivot.localPosition.x + Mathf.Cos(bobbingTimer / 2f) * bobAmount * 0.01f,
			camFollowPivot.localPosition.y + Mathf.Sin(bobbingTimer) * bobAmount,
			camFollowPivot.localPosition.z
		);

	}

	private void StopHeadBob()
	{
		if (camFollowPivot.localPosition == originalCamPos) return;
		camFollowPivot.localPosition = Vector3.Lerp(camFollowPivot.localPosition, originalCamPos, 1 * Time.deltaTime);
	}

	private void StopHeadSway()
	{
		if (camNoiseChannel.AmplitudeGain == 0 || camNoiseChannel.FrequencyGain == 0) return;
		camNoiseChannel.AmplitudeGain = Mathf.Lerp(camNoiseChannel.AmplitudeGain, 0, 1 * Time.deltaTime);
		camNoiseChannel.FrequencyGain = Mathf.Lerp(camNoiseChannel.FrequencyGain, 0, 1 * Time.deltaTime);
	}


	private void HeadBobbing()
	{

		if (camNoiseChannel != null)
		{
			if (playerController.inputDir == Vector2.zero || !playerController.isGrounded)
			{
				movingTimer = 0;
				bobbingTimer = 0;
				StopHeadBob();
				camNoiseChannel.AmplitudeGain = idleBobAmplitude;
				camNoiseChannel.FrequencyGain = idleBobFrequency;

			}
			else
			{
				movingTimer += Time.deltaTime;
				//don't start bobbing until player has moved for a little bit. Prevents weird stutters when quickily tapping move key
				if (movingTimer >= 0.25f)
				{
					StartHeadBob();
					StopHeadSway();
				}
			}
		}

	}

	private void CheckForEnemyVisibility()
	{
		// Check if enemy in camera frustrum
		float renderDistance = Camera.main.farClipPlane;
		Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
		Collider[] enemyColliders = Physics.OverlapSphere(Camera.main.transform.position, renderDistance, enemyLayer);
		foreach (Collider enemyCollider in enemyColliders)
		{
			if (GeometryUtility.TestPlanesAABB(planes, enemyCollider.bounds))
			{
				// Check line of sight
				//Debug.Log("found enemies");
				Vector3 directionToEnemy = (enemyCollider.transform.position - Camera.main.transform.position).normalized;
				float enemyDist = Vector3.Distance(transform.position, enemyCollider.transform.position);
				Debug.DrawRay(Camera.main.transform.position, directionToEnemy * (enemyDist + 1), Color.red);
				// Check field of view
				float angle = Vector3.Angle(Camera.main.transform.forward, directionToEnemy);

				if (angle < peripheralAngle / 2)
				{
					CheckForObstaclesBetweenEnemy(directionToEnemy, enemyDist);
				}
			}
		}
	}

	public bool CheckForLightVisibility(Light light)
	{
		// Get the camera's frustum planes
		Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

		// For point lights, treat it as a small sphere or a box for testing
		if (light.type == LightType.Point)
		{
			// Define the point light's bounding volume (a small sphere for the light)
			Bounds bounds = new Bounds(light.transform.position, Vector3.one * light.range);

			return GeometryUtility.TestPlanesAABB(planes, bounds);
		}
		else if (light.type == LightType.Spot)
		{
			// For a spotlight, you need a more complex bounding box representing the cone
			float spotAngle = light.spotAngle;
			float coneRadius = Mathf.Tan(Mathf.Deg2Rad * spotAngle / 2) * light.range;

			Vector3 boxSize = new Vector3(coneRadius * 2, coneRadius * 2, light.range);
			Bounds bounds = new Bounds(light.transform.position, boxSize);
			return GeometryUtility.TestPlanesAABB(planes, bounds);
		}

		return false;
	}

	private void CheckForObstaclesBetweenEnemy(Vector3 directionToEnemy, float enemyDistance)
	{
		int obstacleLayers = obstacleLayer.value | groundLayer.value | interactableMoveablesLayer;
		if (Physics.Raycast(Camera.main.transform.position, directionToEnemy, out RaycastHit hit, enemyDistance + 1))
		{
			if (((1 << hit.collider.gameObject.layer) & enemyLayer) != 0 && ((1 << hit.collider.gameObject.layer) & obstacleLayers) == 0)
			{
				IReactToPlayerGaze reactableMonster = hit.collider.GetComponent<IReactToPlayerGaze>();
				if (reactableMonster != null)
				{
					//Debug.Log("Enemy Seen ");
					reactableMonster.ReactToPlayerGaze(GetComponent<NetworkObject>());

				}
			}
		}
	}

	void LateUpdate()
	{
		if (playerCam != null && !isDead && canMove)
		{
			Quaternion targetRotation = Quaternion.Euler(0, cmCamPanTilt.PanAxis.Value, 0);
			transform.rotation = targetRotation;

			itemCamera.transform.rotation = mainCameraPosition.rotation;
			HeadBobbing();
		}


	}

	void Update()
	{

		if (playerCam != null && !isDead)
		{
			CheckForEnemyVisibility();
		}
		if (spectatorCam != null && isDead)
		{
			camFollowPivot.transform.position = currentPlayerToSpectate.transform.position;
		}

		if (inputAxisController != null)
		{
			inputAxisController.enabled = canMove;
		}
		if (followZoom != null)
		{
			if (canMove && playerController.currentState == PlayerState.Walking)
			{
				followZoom.FovRange = Vector2.Lerp(followZoom.FovRange, defaultFOV + new Vector2(0, currentFOVSpeedBoost), fovTransitionSpeed * Time.deltaTime);
			}
			else if (canMove && playerController.currentState == PlayerState.Running)
			{
				followZoom.FovRange = Vector2.Lerp(followZoom.FovRange, sprintFOV + new Vector2(0, currentFOVSpeedBoost), fovTransitionSpeed * Time.deltaTime);
			}
			else if (!canMove)
			{
				followZoom.FovRange = Vector2.Lerp(followZoom.FovRange, jumpScareFOV, zoomSmoothTime * Time.deltaTime);
			}
		}


	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		PlayerHealth.onDeath -= DisablePlayerCamera;

	}

	public void ApplyAnimationLock(float animationTime)
	{
		StartCoroutine(AnimationLocked(animationTime));
	}

	private IEnumerator AnimationLocked(float lockTime)
	{
		canMove = false;
		yield return new WaitForSeconds(lockTime);
		canMove = true;
	}

	private void HandleLobbyLoading()
	{
		Debug.Log("PlayerCamera.cs - Unlocking Cursor + Disabling Cameras");
		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;
		if (spectatorCam != null)
		{
			spectatorCam.gameObject.SetActive(false);
		}
		if (playerCam != null)
		{
			playerCam.gameObject.SetActive(true);
		}
	}

}
