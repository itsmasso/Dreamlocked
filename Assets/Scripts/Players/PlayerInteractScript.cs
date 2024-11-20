using UnityEngine;
using FishNet.Object;

using UnityEngine.InputSystem;
using System.Collections;





public class PlayerInteractScript : NetworkBehaviour
{
	[SerializeField] private float interactRange;
	public GameObject itemParent;
	private InteractableObjectBase heldObject;
	
	[SerializeField] private bool pressedInteract;
	[SerializeField] private LayerMask interactableLayer;
	private Transform mainCameraPosition;

	public override void OnStartClient()
	{
		base.OnStartClient();
		if(base.IsOwner)
		{
			mainCameraPosition = Camera.main.transform;
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
			DropObjectServer(heldObject.gameObject);
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
		//mainCameraPosition = Camera.main.transform;
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
				//turn into switch case later on maybe?
				
				//the current interactable is an object that can be picked up
				InteractableObjectBase interactableObj = hit.collider.gameObject.GetComponent<InteractableObjectBase>();
				if(interactableObj != null)
				{
					ObjectInteractServer(hit.collider.gameObject);
					heldObject = interactableObj;
				}

				pressedInteract = false;
			}
		}
		Debug.DrawRay(mainCameraPosition.position, mainCameraPosition.forward * interactRange, Color.red);
	}
	
	[ServerRpc(RequireOwnership = false)]
	private void ObjectInteractServer(GameObject interactableObj)
	{
		ObjectInteractObserver(interactableObj);
	}
	
	[ObserversRpc]
	private void ObjectInteractObserver(GameObject interactableObj)
	{
		interactableObj.GetComponent<InteractableObjectBase>().Interact(itemParent);
	}
	
	[ServerRpc(RequireOwnership = false)]
	private void DropObjectServer(GameObject heldObj)
	{
		DropObjectObserver(heldObj);
	}
	
	[ObserversRpc]
	private void DropObjectObserver(GameObject heldObj)
	{
		heldObj.GetComponent<InteractableObjectBase>().DropItem();
	}
	

	private void Update()
	{
		if(mainCameraPosition != null)
		{
			PlayerInteract();
		}
		
	}
}
