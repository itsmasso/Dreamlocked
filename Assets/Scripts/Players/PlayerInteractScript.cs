using UnityEngine;


using UnityEngine.InputSystem;
using System.Collections;

using Unity.Netcode;
using System.Linq;



public class PlayerInteractScript : NetworkBehaviour
{
	private Transform mainCameraPosition;

	[Header("Interact Properties")]
	[SerializeField] private float interactRange;
	[SerializeField] private float sphereRadius;
	[SerializeField] private LayerMask interactableLayer, interactableMoveableLayer;
	private int interactableLayers;
	[SerializeField] private bool pressedInteract;
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
				IInteractable interactableParent = obj.gameObject.GetComponentInParent<IInteractable>();
				if (interactable != null && pressedInteract)
				{
					interactable.Interact(gameObject.GetComponent<NetworkObject>());

					pressedInteract = false;
					break;
				}
				else if (interactableParent != null && pressedInteract)
				{
					interactableParent.Interact(gameObject.GetComponent<NetworkObject>());

					pressedInteract = false;
					break;
				}
			}
			Debug.DrawRay(mainCameraPosition.position, mainCameraPosition.forward * interactRange, Color.red);
		}
	}

	private void Update()
	{
		PlayerInteract();
	}

}
