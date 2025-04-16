
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
		RequestServerToInteractRpc(playerNetworkObjRef);
	}

	[Rpc(SendTo.Server)]
	protected virtual void RequestServerToInteractRpc(NetworkObjectReference playerNetworkObjRef)
	{
		isStored.Value = false;
		objectRb.constraints = RigidbodyConstraints.None;
		playerNetworkObjRef.TryGet(out NetworkObject playerNetworkObj);
		//playerNetworkObj.GetComponent<PlayerInteractScript>().SpawnVisualItemClientRpc(GetItemSOIndex(itemScriptableObject));
		if(playerNetworkObj.GetComponent<PlayerInventory>().AddItems(itemScriptableObject))
		{
		    HideItem();
		}
	}


	public virtual void ThrowItem(Vector3 direction, float throwForce)
	{
		RequestServerToThrowItemRpc(direction, throwForce);
	}

	[Rpc(SendTo.Server)]
	protected virtual void RequestServerToThrowItemRpc(Vector3 direction, float throwForce)
	{
		AllSeeClientThrowItemRpc(direction, throwForce);
	}

	[Rpc(SendTo.Everyone)]
	protected virtual void AllSeeClientThrowItemRpc(Vector3 direction, float throwForce)
	{
		objectRb.AddForce(direction.normalized * throwForce, ForceMode.VelocityChange);

	}
	public virtual void HideItem()
	{
		Debug.Log("despawned item");
		GetComponent<NetworkObject>().Despawn(true);
	}

	protected int GetItemSOIndex(ItemScriptableObject itemScriptableObject)
	{
		return itemSOList.itemListSO.IndexOf(itemScriptableObject);
	}



}
