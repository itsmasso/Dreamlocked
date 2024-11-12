
using UnityEngine;

public abstract class InteractableObjectBase : MonoBehaviour, IInteractable
{
	[SerializeField] private Rigidbody objectRB;
	[SerializeField] private LayerMask groundLayer;
	protected virtual void Start()
	{
		
	}
	public virtual void Interact(GameObject parent)
	{
		objectRB.isKinematic = true;
		transform.parent = parent.transform;
		transform.localPosition = Vector3.zero;
		transform.localRotation = Quaternion.identity;
		int holdItemLayer = LayerMask.NameToLayer("ItemHold");
		gameObject.layer = holdItemLayer;
		
	}
	
	public virtual void DropItem()
	{
		//RaycastHit hit;
		if(Physics.Raycast(transform.position, Vector3.down, Mathf.Infinity, groundLayer))
		{
			objectRB.isKinematic = false;
			transform.SetParent(null);
			int interactableLayer = LayerMask.NameToLayer("Interactable");
			gameObject.layer = interactableLayer;
			
		}
		Debug.DrawRay(transform.position, Vector3.down * Mathf.Infinity, Color.red);
		
	}
	
	
	
	
}
