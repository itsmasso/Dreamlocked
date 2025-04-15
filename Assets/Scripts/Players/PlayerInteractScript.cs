using UnityEngine;


using UnityEngine.InputSystem;
using System.Collections;

using Unity.Netcode;
using System.Linq;



public class PlayerInteractScript : NetworkBehaviour
{
	private Transform mainCameraPosition;

	[Header("Item Properties")]
	public GameObject itemParent;
	public Transform itemPosition;
	[SerializeField] private ItemListScriptableObject itemSOList;
	[SerializeField] private ItemScriptableObject heldObject;
	private GameObject currentVisualItem;
	[Header("Interact Properties")]
	[SerializeField] private float interactRange;
	[SerializeField] private float sphereRadius;
	[SerializeField] private LayerMask interactableLayer, interactableMoveableLayer;
	private int interactableLayers;
	[SerializeField] private bool pressedInteract;

	[Header("Drop Item Properties")]
	[SerializeField] private float throwForce;
	[SerializeField] private LayerMask groundLayer;
	[SerializeField] private Collider[] hits;

	void Start()
	{
		if (!IsOwner)
		{
			this.enabled = false;
		}
		pressedInteract = false;
		mainCameraPosition = Camera.main.transform;
		interactableLayers = interactableLayer.value | interactableMoveableLayer.value;
	}

	public void OnInteract(InputAction.CallbackContext ctx)
	{
		if (ctx.performed)
		{
			pressedInteract = true;
			StartCoroutine(ResetButtonPressed());
		}

	}

	public void OnDropItem(InputAction.CallbackContext ctx)
	{
		if (ctx.performed && heldObject != null)
		{
			DropItemServerRpc(Camera.main.transform.forward, throwForce);
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
		if (Physics.Raycast(mainCameraPosition.position, mainCameraPosition.forward, out hit, interactRange))
		{
			hits = Physics.OverlapSphere(hit.point, sphereRadius, interactableLayers);
			hits = hits.OrderByDescending(obj => obj.GetComponent<InteractableItemBase>() != null).ToArray();
			foreach (Collider obj in hits)
			{
				//NetworkObject networkObj = obj.gameObject.GetComponent<NetworkObject>();
				IInteractable interactable = obj.gameObject.GetComponent<IInteractable>();

				if (interactable != null && pressedInteract)
				{
					interactable.Interact(gameObject.GetComponent<NetworkObject>());
				
					pressedInteract = false;
					break;
				}
			}
			Debug.DrawRay(mainCameraPosition.position, mainCameraPosition.forward * interactRange, Color.red);
		}
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

	private void Update()
	{
		PlayerInteract();
	}

}
