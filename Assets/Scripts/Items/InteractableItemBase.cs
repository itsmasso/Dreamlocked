
using UnityEngine;
using Unity.Netcode;

public abstract class InteractableItemBase : NetworkBehaviour, IInteractable
{
	[SerializeField] private Rigidbody objectRb;
	[SerializeField] private Collider col;
	public ItemScriptableObject itemScriptableObject;
	public NetworkVariable<bool> isStored = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	private ItemData itemData;
	protected virtual void Start()
	{
		if (IsServer)
		{
			isStored.Value = false;
		}
	}
	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
	}

    public void InitializeItemData(ItemData data)
	{
		if (!IsServer) return;
		itemData = data;
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
		if (playerNetworkObj.GetComponent<PlayerInventory>().AddItems(itemData))
		{
			DespawnItem();
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
		Debug.Log("throwing item");
		
		GetComponent<NetworkObject>().TrySetParent((Transform)null, true);
		transform.parent = null;
		objectRb.isKinematic = false;
		GetComponent<Collider>().enabled = true;
		GetComponent<Collider>().isTrigger = false;
		objectRb.AddForce(direction.normalized * throwForce, ForceMode.VelocityChange);
	}

	public virtual void DespawnItem()
	{
		GetComponent<NetworkObject>().Despawn(true);
	}


}
