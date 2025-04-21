using System;
using System.Collections;
using System.Collections.Generic;
using Steamworks.Ugc;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInventory : NetworkBehaviour
{
    public static event Action<int> onNewSlotSelected;
    public static event Action<int> onDropItem;
    public static event Action<int, int> onAddItem;
    [SerializeField] private NetworkList<ItemData> syncedInventory = new NetworkList<ItemData>();
    //private ItemScriptableObject currentActiveItem;
    [SerializeField] private int currentInventoryIndex;
    [SerializeField] private int lastInventoryIndex = -1;

    [Header("Item Properties")]
    [SerializeField] private GameObject itemParent;
    [SerializeField] private Transform itemTransform;
    [SerializeField] private GameObject currentUseableItem;
    private GameObject currentVisualItem;
    public ItemData currentHeldItemData;
    [Header("Drop Item Properties")]
    [SerializeField] private float throwForce;

    void Start()
    {
        currentInventoryIndex = 0;
        if (IsServer)
        {
            ItemData emptyItem = new ItemData
            {
                id = -1,
                itemCharge = 0,
                usesRemaining = 0
            };

            while (syncedInventory.Count < 4)
                syncedInventory.Add(emptyItem); //adding empty slots
        }

    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    public void UseItem(InputAction.CallbackContext ctx)
    {
        if (ctx.performed && currentUseableItem != null)
        {
            if (currentUseableItem.TryGetComponent(out IUseableItem<ItemData> activatableItem))
            {
                activatableItem.UseItem();
            }
        }
    }

    public void OnSlot1(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            RequestServerToSelectNewItemRpc(0);
        }

    }
    public void OnSlot2(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            RequestServerToSelectNewItemRpc(1);
        }

    }
    public void OnSlot3(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            RequestServerToSelectNewItemRpc(2);
        }

    }
    public void OnSlot4(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            RequestServerToSelectNewItemRpc(3);
        }

    }

    public void OnDropItem(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            RequestServerToDropItemRpc(Camera.main.transform.forward, throwForce);
        }
    }
    public bool AddItems(ItemData itemData)
    {
        if (!IsServer) return false;

        // If current slot is full, add to the first available empty slot

        for (int i = 0; i < syncedInventory.Count; i++)
        {
            if (syncedInventory[i].id == -1)
            {
                if (currentInventoryIndex == i)
                {
                    SpawnVisualItemRpc(itemData);
                    RequestToSpawnUseableItemRpc(itemData);

                }
                OwnerUpdatesSpriteRpc(i, itemData.id);

                syncedInventory[i] = itemData;
                return true;
            }
        }

        return false;
    }

    [Rpc(SendTo.Owner)]
    private void OwnerUpdatesSpriteRpc(int slotNumber, int id)
    {
        onAddItem?.Invoke(slotNumber, id);
    }
    private void HandleHeldItemDataChanged(ItemData newData)
    {
        if (!IsServer) return;
        syncedInventory[currentInventoryIndex] = newData;
        currentHeldItemData = newData;
    }

    [Rpc(SendTo.Everyone)]
    private void SpawnVisualItemRpc(ItemData itemData)
    {
        var itemSO = ItemDatabase.Get(itemData.id);
        GameObject visualItemPrefab = itemSO?.visualPrefab;
        GameObject visualItem = Instantiate(visualItemPrefab, itemTransform.position, itemTransform.rotation);
        visualItem.transform.SetParent(itemTransform, false);
        visualItem.transform.localPosition = Vector3.zero;
        visualItem.transform.localRotation = Quaternion.LookRotation(Vector3.forward);
        currentVisualItem = visualItem;
        currentHeldItemData = itemData;
    }
    [Rpc(SendTo.Everyone)]
    private void DestroyVisualItemRpc()
    {
        if (currentVisualItem != null)
        {
            Destroy(currentVisualItem);
            currentVisualItem = null;
        }
    }


    [Rpc(SendTo.Server)]
    private void RequestToSpawnUseableItemRpc(ItemData itemData)
    {
        var itemSO = ItemDatabase.Get(itemData.id);
        if (itemSO.isUseable)
        {
            GameObject useableItemPrefab = itemSO?.useablePrefab;
            currentUseableItem = Instantiate(useableItemPrefab, itemTransform.position, itemTransform.rotation);
            NetworkObject useableNetObject = currentUseableItem.GetComponent<NetworkObject>();
            useableNetObject.SpawnWithOwnership(OwnerClientId);
            useableNetObject.TrySetParent(gameObject, false);
            OwnerSetsCurrentHeldItemRpc(useableNetObject);
            IUseableItem<ItemData> useableItem = currentUseableItem.GetComponent<IUseableItem<ItemData>>();
            if (useableItem != null)
            {
                useableItem.InitializeData(itemData);
                useableItem.OnDataChanged += HandleHeldItemDataChanged;
                currentHeldItemData = itemData;
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestServerToDespawnUseableItemRpc()
    {
        if (currentUseableItem == null) return;

        if (currentUseableItem.TryGetComponent<NetworkObject>(out var netObj))
        {
            if (netObj.IsSpawned)
            {
                netObj.Despawn(true);
            }
        }
    }

    [Rpc(SendTo.Owner)]
    private void OwnerSetsCurrentHeldItemRpc(NetworkObjectReference networkObjectReference)
    {
        if (networkObjectReference.TryGet(out NetworkObject networkObject) && networkObject != null)
        {
            currentUseableItem = networkObject.gameObject;

            currentUseableItem.transform.localPosition = Vector3.zero;
            currentUseableItem.transform.rotation = Quaternion.identity;
        }
    }

    void Update()
    {

    }

    [Rpc(SendTo.Server)]
    private void RequestServerToSelectNewItemRpc(int inventoryIndex)
    {
        if (inventoryIndex >= syncedInventory.Count)
            return;

        ItemData selectedItem = syncedInventory[inventoryIndex];

        // Avoid unnecessary swaps if same slot and same item type
        if (inventoryIndex == lastInventoryIndex &&
            currentHeldItemData.id == selectedItem.id) // if you're tracking unique ID
        {
            return;
        }

        currentInventoryIndex = inventoryIndex;
        lastInventoryIndex = inventoryIndex;

        OwnerHighlightsNewSlotRpc(inventoryIndex);

        // If slot contains a valid item, spawn it
        if (selectedItem.id != -1)
        {
            RequestServerToDespawnUseableItemRpc();
            DestroyVisualItemRpc();

            RequestToSpawnUseableItemRpc(selectedItem);
            SpawnVisualItemRpc(selectedItem);
        }
        else
        {
            RequestServerToDespawnUseableItemRpc();
            DestroyVisualItemRpc();
        }

    }
    [Rpc(SendTo.Owner)]
    private void OwnerHighlightsNewSlotRpc(int inventoryIndex)
    {
        onNewSlotSelected?.Invoke(inventoryIndex);
    }


    [Rpc(SendTo.Server)]
    private void RequestServerToDropItemRpc(Vector3 dropDirection, float throwForce)
    {
        ItemData emptyItem = new ItemData { id = -1, itemCharge = 0, usesRemaining = 0 };

        if (syncedInventory[currentInventoryIndex].id != -1)
        {
            // Clean visual + networked object
            RequestServerToDespawnUseableItemRpc();
            DestroyVisualItemRpc();

            // Spawn drop in world
            var dropPrefab = ItemDatabase.Get(syncedInventory[currentInventoryIndex].id).droppablePrefab;
            var droppedItem = Instantiate(dropPrefab, itemTransform.position, Quaternion.identity);
            droppedItem.GetComponent<NetworkObject>().Spawn(true);
            droppedItem.GetComponent<InteractableItemBase>().InitializeItemData(currentHeldItemData);
            droppedItem.GetComponent<InteractableItemBase>().ThrowItem(dropDirection, throwForce);

            // Clear inventory
            syncedInventory[currentInventoryIndex] = emptyItem;
            currentUseableItem = null;
            currentHeldItemData = emptyItem;
            lastInventoryIndex = -1;

            // Notify client to remove sprite
            OwnerRemovesSpriteRpc(currentInventoryIndex);
        }
    }

    [Rpc(SendTo.Server)]
    public void RequestServerToDestroyItemRpc()
    {
        ItemData emptyItem = new ItemData { id = -1, itemCharge = 0, usesRemaining = 0 };

        if (syncedInventory[currentInventoryIndex].id != -1)
        {
            // Clean visual + networked object
            RequestServerToDespawnUseableItemRpc();
            IHasDestroyAnimation hasDestroyAnimation = currentVisualItem.GetComponent<IHasDestroyAnimation>();
            if(hasDestroyAnimation != null)
            {
                hasDestroyAnimation.PlayDestroyAnimation();
            }else
            {
                DestroyVisualItemRpc();
            }
            
            // Clear inventory
            syncedInventory[currentInventoryIndex] = emptyItem;
            currentUseableItem = null;
            currentHeldItemData = emptyItem;
            lastInventoryIndex = -1;

            // Notify client to remove sprite
            OwnerRemovesSpriteRpc(currentInventoryIndex);
        }

    }

    public GameObject GetCurrentUsableItem()
    {
        return currentUseableItem;
    }

    public GameObject GetCurrentVisualItem()
    {
        return currentVisualItem;
    }


    [Rpc(SendTo.Owner)]
    private void OwnerRemovesSpriteRpc(int slot)
    {
        onDropItem?.Invoke(slot);
    }


}
