using System;
using System.Collections.Generic;
using Steamworks.Ugc;
using Unity.Netcode;
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
    [SerializeField] private GameObject currentHeldItemObject;
    private ItemData currentHeldItemData;
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
        if (ctx.performed)
        {
            IUseableItem<ItemData> activatableItem = currentHeldItemObject.GetComponent<IUseableItem<ItemData>>();
            if (activatableItem != null)
            {
                activatableItem.UseItem();
            }
        }
    }

    public void OnSlot1(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            NewItemSelectedRpc(0);
        }

    }
    public void OnSlot2(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            NewItemSelectedRpc(1);
        }

    }
    public void OnSlot3(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            NewItemSelectedRpc(2);
        }

    }
    public void OnSlot4(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            NewItemSelectedRpc(3);
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
                    SpawnHeldItem(itemData);
                    
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
    private void SpawnHeldItem(ItemData itemData)
    {
        if (!IsServer) return;

        GameObject prefab = ItemDatabase.Get(itemData.id).heldPrefab;
        if (prefab.GetComponent<NetworkObject>() != null)
        {
            currentHeldItemObject = Instantiate(prefab, itemTransform.position, itemTransform.rotation);            
            NetworkObject heldItemNetObj = currentHeldItemObject.GetComponent<NetworkObject>();
            heldItemNetObj.Spawn(true);
            OwnerSetCurrentHeldItemRpc(heldItemNetObj);
            heldItemNetObj.TrySetParent(gameObject);
            IUseableItem<ItemData> useableItem = currentHeldItemObject.GetComponent<IUseableItem<ItemData>>();
            if (useableItem != null)
            {
                useableItem.InitializeData(itemData);
                useableItem.OnDataChanged += HandleHeldItemDataChanged;
                currentHeldItemData = itemData;
            }
        }else
        {
            EveryoneSpawnsHeldItemRpc(itemData.id);
        }

    }

    [Rpc(SendTo.Everyone)]
    private void EveryoneSpawnsHeldItemRpc(int id)
    {
        currentHeldItemObject = Instantiate(ItemDatabase.Get(id).heldPrefab, itemTransform.position, itemTransform.rotation);
        currentHeldItemObject.transform.SetParent(transform);
    }

    [Rpc(SendTo.Owner)]
    private void OwnerSetCurrentHeldItemRpc(NetworkObjectReference networkObjectReference)
    {
        if(networkObjectReference.TryGet(out NetworkObject networkObject))
        {
            currentHeldItemObject = networkObject.gameObject;
        }
    }

    [Rpc(SendTo.Server)]
    private void NewItemSelectedRpc(int inventoryIndex)
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
            HideItem();
            SpawnHeldItem(selectedItem);
        }
        else
        {
            HideItem();
        }

    }
    [Rpc(SendTo.Owner)]
    private void OwnerHighlightsNewSlotRpc(int inventoryIndex)
    {
        onNewSlotSelected?.Invoke(inventoryIndex);
    }


    [Rpc(SendTo.Server)]
    private void RequestServerToDropItemRpc(Vector3 dropPosition, float throwForce)
    {
        ItemData emptyItem = new ItemData
        {
            id = -1,
            itemCharge = 0,
            usesRemaining = 0
        };
        if (syncedInventory[currentInventoryIndex].id != -1)
        {
            HideItem();
            GameObject itemToThrow = Instantiate(ItemDatabase.Get(currentHeldItemData.id).droppablePrefab, itemTransform.position, Quaternion.identity);
            itemToThrow.GetComponent<NetworkObject>().Spawn(true);
            itemToThrow.GetComponent<InteractableItemBase>().InitializeItemData(currentHeldItemData);
              
            itemToThrow.GetComponent<InteractableItemBase>().ThrowItem(dropPosition, throwForce);
            OwnerRemovesSpriteRpc();
            currentHeldItemObject = null;
            currentHeldItemData = emptyItem;
            syncedInventory[currentInventoryIndex] = emptyItem;
            lastInventoryIndex = -1;
        }
    }
    
    [Rpc(SendTo.Owner)]
    private void OwnerRemovesSpriteRpc()
    {
        onDropItem?.Invoke(currentInventoryIndex);
    }

    private void HideItem()
    {
        if (currentHeldItemObject != null && currentHeldItemObject.GetComponent<NetworkObject>() != null)
        {
            currentHeldItemObject.GetComponent<NetworkObject>().Despawn(true);
        }
        else
        {
            OwnerDestroysHeldObjRpc();
        }
    }

    [Rpc(SendTo.Everyone)]
    private void OwnerDestroysHeldObjRpc()
    {
        Destroy(currentHeldItemObject);
    }
}
