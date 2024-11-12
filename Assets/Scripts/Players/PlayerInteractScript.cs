using UnityEngine;
using FishNet.Object;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.Rendering.Universal;
using Unity.VisualScripting;


public class PlayerInteractScript : NetworkBehaviour
{
	[SerializeField] private float interactRange;
	private Transform mainCameraPosition;
	[SerializeField] private Camera holdCamera;
	[SerializeField] private GameObject itemPosition;
	private InteractableObjectBase heldObject;
	
	[SerializeField] private bool pressedInteract;
	[SerializeField] private LayerMask interactableLayer;
	[SerializeField] private float itemOrbitDistance = 1f;

	public override void OnStartClient()
	{
		base.OnStartClient();
		if(base.IsOwner)
		{
			mainCameraPosition = Camera.main.transform;
			Camera.main.GetUniversalAdditionalCameraData().cameraStack.Add(holdCamera);
			holdCamera.enabled = true;
		}
		else
		{
			holdCamera.enabled = false;
			this.enabled = false;
		}
	}
	
	public void OnInteract(InputAction.CallbackContext ctx)
	{
		if(ctx.performed)
		{
			pressedInteract = true;		
			StartCoroutine(ResetButtonPressed());
		}

	}
	
	public void OnDropItem(InputAction.CallbackContext ctx)
	{
		if(ctx.performed && heldObject != null)
		{
			heldObject.DropItem();
			heldObject = null;
		}
	}
	
	private IEnumerator ResetButtonPressed()
	{
		yield return new WaitForSeconds(0.2f);
		pressedInteract = false;
	}
	
	void Start()
	{
		pressedInteract = false;
	}
	
	private void PlayerInteract()
	{
		if(mainCameraPosition != null)
		{
			
			RaycastHit hit;
			if(Physics.Raycast(mainCameraPosition.position, mainCameraPosition.forward, out hit, interactRange, interactableLayer))
			{
				IInteractable interactable = hit.collider.gameObject.GetComponent<IInteractable>();
				Debug.Log("raycast hit");
				if(interactable != null && pressedInteract)
				{
					interactable.Interact(itemPosition);
					heldObject = hit.collider.gameObject.GetComponent<InteractableObjectBase>();
					pressedInteract = false;
				}
			}
			Debug.DrawRay(mainCameraPosition.position, mainCameraPosition.forward * interactRange, Color.red);
		}
	}

	void Update()
	{
		PlayerInteract();
		if(mainCameraPosition != null)
		{
			holdCamera.transform.localRotation = mainCameraPosition.localRotation;
			//Vector3 itemPositionOffset = mainCameraPosition.localRotation * Vector3.forward * itemOrbitDistance;
			//itemPosition.transform.position = mainCameraPosition.position + itemPositionOffset;
			//itemPosition.transform.LookAt(mainCameraPosition);
		}
		
		
	}
}
