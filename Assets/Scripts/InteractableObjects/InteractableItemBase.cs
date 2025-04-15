
using UnityEngine;
using Unity.Netcode;

public abstract class InteractableItemBase : NetworkBehaviour, IInteractable
{
	[SerializeField] private Rigidbody objectRb;
	public ItemScriptableObject itemScriptableObject;
	[SerializeField] private ItemListScriptableObject itemSOList;
	public NetworkVariable<bool> isStored = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

	void Awake()
	{
		isStored.Value = false;

	}
	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
	}

	public virtual void Interact(NetworkObjectReference playerNetworkObjRef)
	{

		InteractServerRpc(playerNetworkObjRef);
	}

	[ServerRpc(RequireOwnership = false)]
	protected virtual void InteractServerRpc(NetworkObjectReference playerNetworkObjRef)
	{

		isStored.Value = false;
		objectRb.constraints = RigidbodyConstraints.None;
		playerNetworkObjRef.TryGet(out NetworkObject playerNetworkObj);
		//playerNetworkObj.GetComponent<PlayerInteractScript>().SpawnVisualItemClientRpc(GetItemSOIndex(itemScriptableObject));
		if(playerNetworkObj.GetComponent<PlayerInventory>().AddItems(itemScriptableObject))
		{
		    HideItemServerRpc();
		}
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
	public virtual void HideItemServerRpc()
	{
		Debug.Log("despawned item");
		GetComponent<NetworkObject>().Despawn(true);
	}

	protected int GetItemSOIndex(ItemScriptableObject itemScriptableObject)
	{
		return itemSOList.itemListSO.IndexOf(itemScriptableObject);
	}



}
