using UnityEngine;
using FishNet.Object;

using UnityEngine.InputSystem;
using System.Collections;




public class PlayerInteractScript : NetworkBehaviour
{
	[SerializeField] private float interactRange;
	[SerializeField] private GameObject itemPosition;
	private InteractableObjectBase heldObject;
	
	[SerializeField] private bool pressedInteract;
	[SerializeField] private LayerMask interactableLayer;
	private Transform mainCameraPosition;

	public override void OnStartClient()
	{
		base.OnStartClient();
		if(base.IsOwner)
		{
			
		}
		else
		{
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
		mainCameraPosition = Camera.main.transform;
	}
	
	private void PlayerInteract()
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

	void Update()
	{
		PlayerInteract();
		
	}
}
