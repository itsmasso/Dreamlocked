
using UnityEngine;
using UnityEngine.InputSystem;
using FishNet.Object;
using FishNet.Connection;
using Unity.Cinemachine;
using UnityEngine.Rendering.Universal;

public class PlayerCamera : NetworkBehaviour
{
	
	private PlayerController playerController;
	[SerializeField] private MeshRenderer playerMesh;
	[Header("Camera Properties")]
	private Transform mainCameraPosition;
	private CinemachineCamera cmCam;
	[SerializeField] private Camera itemCamera;
	[SerializeField] private Transform camFollowPivot;
	
	[SerializeField] private Transform itemPivot;
	
	[Header("Head Sway")]
	private CinemachineBasicMultiChannelPerlin camNoiseChannel;
	[SerializeField] private float idleBobAmplitude = 0.2f, idleBobFrequency = 0.4f;
	
	[Header("Head Bob")]
	[SerializeField] private float walkBobSpeed = 14f;
	[SerializeField] private float walkBobAmount = 0.05f; //how much the camera moves
	[SerializeField] private float sprintBobSpeed = 18f;
	[SerializeField] private float sprintBobAmount = 0.1f; //how much the camera moves
	[SerializeField] private float crouchBobSpeed = 8f;
	[SerializeField] private float crouchBobAmount = 0.025f; //how much the camera moves
	[SerializeField] private Vector3 originalCamPos;
	private float bobbingTimer;
	private float movingTimer;
	[SerializeField] private float bobDuration = 2.0f;
	
	
	

	public override void OnStartClient()
	{
		base.OnStartClient();
		if(base.IsOwner)
		{
			cmCam = FindFirstObjectByType<CinemachineCamera>();
			camNoiseChannel = cmCam.GetComponentInChildren<CinemachineBasicMultiChannelPerlin>();
			playerMesh.enabled = false;
			if(cmCam != null)
			{
				cmCam.Follow = camFollowPivot;
			}
			
		}
		else
		{
			
			camFollowPivot.gameObject.SetActive(false);
			playerMesh.enabled = true;
			this.enabled = false;
		}
		
	}
	
	void Start()
	{
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
		playerController = gameObject.GetComponent<PlayerController>();
		Invoke("SetDefaultPos", 1.5f); //maybe make sure to prevent player movement after 1.5 seconds
		mainCameraPosition = Camera.main.transform;
		Camera.main.GetUniversalAdditionalCameraData().cameraStack.Add(itemCamera);

	}
	
	private void SetDefaultPos()
	{
		originalCamPos = camFollowPivot.localPosition;
	}

	private void StartHeadBob()
	{
		bobbingTimer += Time.deltaTime * (playerController.currentState == PlayerState.Crouching ? crouchBobSpeed : playerController.currentState == PlayerState.Running ? sprintBobSpeed : walkBobSpeed);
	
		float bobAmount = playerController.currentState == PlayerState.Crouching ? crouchBobAmount : playerController.currentState == PlayerState.Running ? sprintBobAmount : walkBobAmount;
		camFollowPivot.localPosition = new Vector3(
			camFollowPivot.localPosition.x + Mathf.Cos(bobbingTimer/2f) * bobAmount * 0.05f,
			camFollowPivot.localPosition.y + Mathf.Sin(bobbingTimer) * bobAmount,
			camFollowPivot.localPosition.z
		);
		
	}
	
	private void StopHeadBob()
	{
		if(camFollowPivot.localPosition == originalCamPos) return;
		camFollowPivot.localPosition = Vector3.Lerp(camFollowPivot.localPosition, originalCamPos, 1 * Time.deltaTime);
	}

	private void StopHeadSway()
	{
		if(camNoiseChannel.AmplitudeGain == 0 || camNoiseChannel.FrequencyGain == 0) return;
		camNoiseChannel.AmplitudeGain = Mathf.Lerp(camNoiseChannel.AmplitudeGain, 0, 1 * Time.deltaTime);
		camNoiseChannel.FrequencyGain = Mathf.Lerp(camNoiseChannel.FrequencyGain, 0, 1 * Time.deltaTime);
	}
	
	
	private void HeadBobbing()
	{
		
		if(camNoiseChannel != null)
		{	
			if(playerController.inputDir == Vector2.zero || !playerController.isGrounded)
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
				if(movingTimer >= 0.25f)
				{
					StartHeadBob();
					StopHeadSway();
				}
			}
		}

	}
	
	void LateUpdate()
	{	
		itemCamera.transform.rotation = mainCameraPosition.rotation;
		itemPivot.localRotation = mainCameraPosition.rotation;
		HeadBobbing();

	}

}
