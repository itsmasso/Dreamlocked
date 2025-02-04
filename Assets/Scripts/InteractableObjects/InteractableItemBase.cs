
using UnityEngine;
using Unity.Netcode;

public abstract class InteractableItemBase : NetworkBehaviour
{
	[SerializeField] private Rigidbody objectRb;
	public ItemScriptableObject itemScriptableObject;
	[SerializeField] private ItemListScriptableObject itemSOList;
	[SerializeField] private NetworkObject networkObject;
	
	public virtual void Interact(NetworkObjectReference playerNetworkObjRef)
	{
		InteractServerRpc(playerNetworkObjRef);
	}
	
	[ServerRpc(RequireOwnership = false)]
	protected virtual void InteractServerRpc(NetworkObjectReference playerNetworkObjRef)
	{
		HideItemServerRpc();
		playerNetworkObjRef.TryGet(out NetworkObject playerNetworkObj);
		playerNetworkObj.GetComponent<PlayerInteractScript>().SpawnVisualItemClientRpc(GetItemSOIndex(itemScriptableObject));
	}
	
	public virtual void ThrowItem(Vector3 target, float throwForce)
	{
		ThrowItemServerRpc(target, throwForce);
	}
	
	[ServerRpc]
	protected virtual void ThrowItemServerRpc(Vector3 target, float throwForce)
	{
		ThrowItemClientRpc(target, throwForce);
	}
	
	[ClientRpc]
	protected virtual void ThrowItemClientRpc(Vector3 target, float throwForce)
	{
		Vector3 direction = (target - transform.position).normalized;
		objectRb.AddForce(direction * throwForce, ForceMode.VelocityChange);
		
	}
	
	[ServerRpc]
	protected virtual void HideItemServerRpc()
	{
		networkObject.Despawn(true);
	}
	
	protected int GetItemSOIndex(ItemScriptableObject itemScriptableObject)
	{
		return itemSOList.itemListSO.IndexOf(itemScriptableObject);
	}
	
	
	
}
