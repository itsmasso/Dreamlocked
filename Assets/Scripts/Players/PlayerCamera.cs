
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerCamera : MonoBehaviour
{
	private Vector2 mouseDir;
	[SerializeField] private float mouseSensitivity = 10f;
	[SerializeField] private float rotationSmoothTime = 0.1f;
	private float xRotation; //vertical rotation (rotation around the x axis)
	private float yRotation; //horizontal rotation (rotation around the y axis)
	[SerializeField] private float maxVerticalRotation, minVerticalRotation;
	[SerializeField] private Transform playerTransform;

	[Header("Crouch")]
	[SerializeField] private float crouchHeightReduction;
	private float originalCameraHeight;
	private bool isCrouching;
	[SerializeField] private float crouchSmoothTime = 0.075f;

	void Start()
	{
		isCrouching = false;
		originalCameraHeight = transform.localPosition.y;
		Cursor.lockState = CursorLockMode.Locked;
		PlayerMovement.onCrouch += Crouch;
	}

	private void Crouch(bool enableCrouch)
	{
		isCrouching = enableCrouch;
	}

	public void OnMouseMove(InputAction.CallbackContext ctx)
	{
		mouseDir = ctx.ReadValue<Vector2>();
	}
	
	void Update()
	{
		if(isCrouching)
		{
			transform.localPosition = Vector3.Lerp(transform.localPosition, 
			new Vector3(transform.localPosition.x, originalCameraHeight - crouchHeightReduction, transform.localPosition.z), crouchSmoothTime);
		
		}
		else
		{
			transform.localPosition = Vector3.Lerp(transform.localPosition, 
			new Vector3(transform.localPosition.x, originalCameraHeight, transform.localPosition.z), crouchSmoothTime);
		}


		Vector2 mouseVelocity = mouseDir * mouseSensitivity * Time.deltaTime; //getting mouse movement velocity
		xRotation -= mouseVelocity.y; //rotating vertically based on mouse speed
		xRotation = Mathf.Clamp(xRotation, minVerticalRotation, maxVerticalRotation); //clamping vertical rotation between two values
		transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(xRotation, 0f, 0f), rotationSmoothTime);

		yRotation += mouseVelocity.x; 
		playerTransform.localRotation = Quaternion.Lerp(playerTransform.localRotation, Quaternion.Euler(0f, yRotation, 0f), rotationSmoothTime);

	}

	private void OnDestroy() 
	{
		PlayerMovement.onCrouch -= Crouch;
	}
}
