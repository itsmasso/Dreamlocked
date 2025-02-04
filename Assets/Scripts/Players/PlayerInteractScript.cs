using UnityEngine;


using UnityEngine.InputSystem;
using System.Collections;

using Unity.Netcode;



public class PlayerInteractScript : NetworkBehaviour
{
	private Transform mainCameraPosition;
	
	[Header("Item Properties")]
	public GameObject itemParent;
	public Transform itemPosition;
	[SerializeField] private ItemListScriptableObject itemSOList;
	private ItemScriptableObject heldObject;
	private GameObject currentVisualItem;
	[Header("Interact Properties")]
	[SerializeField] private float interactRange;
	[SerializeField] private LayerMask interactableLayer;
	private bool pressedInteract;
	
	[Header("Drop Item Properties")]
	[SerializeField] private float throwForce;
	[SerializeField] private LayerMask groundLayer;
	
	
	void Start()
	{
		if(!IsOwner)
		{
			this.enabled = false;
		}
		pressedInteract = false;
		mainCameraPosition = Camera.main.transform;
	}
	
	public void OnInteract(InputAction.CallbackContext ctx)
	{
		if(ctx.performed)
		{
			pressedInteract = true;	
			PlayerInteract();	
			StartCoroutine(ResetButtonPressed());
		}

	}
	
	public void OnDropItem(InputAction.CallbackContext ctx)
	{
		if(ctx.performed && heldObject != null)
		{
			RaycastHit hit;
			if(Physics.Raycast(mainCameraPosition.position, mainCameraPosition.forward, out hit, interactRange, groundLayer))
			{
				DropItemServerRpc(hit.point, throwForce);
				
			}
			Debug.DrawRay(mainCameraPosition.position, mainCameraPosition.forward * interactRange, Color.red);
		}
	}
	
	private IEnumerator ResetButtonPressed()
	{
		yield return new WaitForSeconds(0.2f);
		pressedInteract = false;
	}
	

	private void PlayerInteract()
	{
		RaycastHit hit;
		if(Physics.Raycast(mainCameraPosition.position, mainCameraPosition.forward, out hit, interactRange, interactableLayer))
		{
			NetworkObject networkObj = hit.collider.GetComponent<NetworkObject>();
			InteractableItemBase item = networkObj.GetComponent<InteractableItemBase>();
			
			if(item != null && pressedInteract)
			{
				//turn into switch case later on maybe?
				
				//the current interactable is an object that can be picked up
				if(item != null)
				{
					item.Interact(gameObject.GetComponent<NetworkObject>());
					
				}

				pressedInteract = false;
			}
		}
		Debug.DrawRay(mainCameraPosition.position, mainCameraPosition.forward * interactRange, Color.red);
	}
	
	[ClientRpc]
	public void SpawnVisualItemClientRpc(int itemSOIndex)
	{
		GameObject visualItem = Instantiate(itemSOList.itemListSO[itemSOIndex].visualItemPrefab, itemPosition.position, Quaternion.identity);
		visualItem.transform.SetParent(itemPosition);
		currentVisualItem = visualItem;
		heldObject = itemSOList.itemListSO[itemSOIndex];
	}
	
	[ServerRpc]
	private void DropItemServerRpc(Vector3 dropPosition, float throwForce)
	{
		GameObject currentItem = Instantiate(heldObject.physicalItemPrefab, itemPosition.position, Quaternion.identity);
		currentItem.GetComponent<NetworkObject>().Spawn(true);
		currentItem.GetComponent<InteractableItemBase>().ThrowItem(dropPosition, throwForce);
		DropItemClientRpc();
	}
	
	[ClientRpc]
	private void DropItemClientRpc()
	{
		Destroy(currentVisualItem);
		heldObject = null;
	}
	
}
