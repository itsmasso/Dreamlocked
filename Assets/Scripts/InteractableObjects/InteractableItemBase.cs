
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
	
	public virtual void ThrowItem(Vector3 direction, float throwForce)
	{
		ThrowItemServerRpc(direction, throwForce);
	}
	
	[ServerRpc]
	protected virtual void ThrowItemServerRpc(Vector3 direction, float throwForce)
	{
		ThrowItemClientRpc(direction, throwForce);
	}
	
	[ClientRpc]
	protected virtual void ThrowItemClientRpc(Vector3 direction, float throwForce)
	{
		objectRb.AddForce(direction.normalized * throwForce, ForceMode.VelocityChange);
		
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
