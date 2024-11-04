
using UnityEngine;
using UnityEngine.InputSystem;
using FishNet.Object;
using FishNet.Connection;
using Unity.Cinemachine;

public class PlayerCamera : NetworkBehaviour
{
	[Header("Camera Properties")]
	[SerializeField] private CinemachineCamera playerCam;
	[SerializeField] private Transform camPosition;
	[SerializeField] private float rotationSmoothTime = 0.1f;
	[SerializeField] private float maxVerticalRotation = 60f, minVerticalRotation = -60f;
	
	private float xRotation; //vertical rotation (rotation around the x axis)
	private float yRotation; //horizontal rotation (rotation around the y axis)
	
	[Header("Mouse")]
	[SerializeField] private float mouseSensitivity = 10f;
	private Vector2 mouseDir;

	[Header("Crouch")]
	[SerializeField] private float crouchHeightReduction;
	private bool isCrouching;
	[SerializeField] private float crouchSmoothTime = 0.075f;
	

	public override void OnStartClient()
	{
		base.OnStartClient();
		if(base.IsOwner)
		{
			playerCam.Priority = 1;
		}
		else
		{
			playerCam.Priority = 0;
			this.enabled = false;
		}
	}
	
	void Start()
	{
		isCrouching = false;
		Cursor.lockState = CursorLockMode.Locked;
		//PlayerMovement.onCrouch += Crouch;
	}

	private void Crouch(bool enableCrouch)
	{
		isCrouching = enableCrouch;
	}

	public void OnMouseMove(InputAction.CallbackContext ctx)
	{
		mouseDir = ctx.ReadValue<Vector2>();
	}
	
	void LateUpdate()
	{
		if(isCrouching)
		{
			Vector3 targetPos = new Vector3(camPosition.position.x, transform.position.y - crouchHeightReduction, camPosition.position.z);
			camPosition.position = Vector3.Lerp(camPosition.position, targetPos, crouchSmoothTime);
		
		}
		else
		{
			Vector3 targetPos = new Vector3(camPosition.position.x, transform.position.y + crouchHeightReduction, camPosition.position.z);
			camPosition.position = Vector3.Lerp(camPosition.position, targetPos, crouchSmoothTime);
		}


		Vector2 mouseVelocity = mouseDir * mouseSensitivity * Time.deltaTime; //getting mouse movement velocity
		xRotation -= mouseVelocity.y; //rotating vertically based on mouse speed
		xRotation = Mathf.Clamp(xRotation, minVerticalRotation, maxVerticalRotation); //clamping vertical rotation between two values
		yRotation += mouseVelocity.x; 
		
		transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, yRotation, 0f), rotationSmoothTime);
		camPosition.localRotation = Quaternion.Lerp(camPosition.localRotation, Quaternion.Euler(xRotation, camPosition.localRotation.y, 0f), rotationSmoothTime);


	}

	private void OnDestroy() 
	{
		//PlayerMovement.onCrouch -= Crouch;
	}
}
