using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInventory : NetworkBehaviour
{
    public static event Action<int> onNewSlotSelected;
    public static event Action<int> onDropItem;
    public static event Action<int, int> onAddItem;
    [SerializeField] private ItemListScriptableObject itemScriptableObjList;
    [SerializeField] private NetworkList<int> syncedInventory = new NetworkList<int>();
    //private ItemScriptableObject currentActiveItem;
    [SerializeField] private int currentInventoryIndex;
    [SerializeField] private int lastInventoryIndex = -1;

    [Header("Item Properties")]
    [SerializeField] private GameObject itemParent;
    [SerializeField] private Transform itemPosition;
    [Tooltip("Don't initialize held object.")]
    [SerializeField] private ItemScriptableObject heldObject;
    private GameObject currentVisualItem;
    [Header("Drop Item Properties")]
    [SerializeField] private float throwForce;
    //[SerializeField] private LayerMask groundLayer;

    void Start()
    {
        currentInventoryIndex = 0;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            while (syncedInventory.Count < 4)
                syncedInventory.Add(-1);
        }

        if (IsClient)
        {
            syncedInventory.OnListChanged += OnInventoryChanged;
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
        if (ctx.performed && heldObject != null)
        {
            DropItemRpc(Camera.main.transform.forward, throwForce);
        }
    }
    public bool AddItems(ItemScriptableObject itemScriptableObject)
    {
        if (!IsServer) return false;

        int itemIndex = GetItemSOIndex(itemScriptableObject);
        // If current slot is full, add to the first available empty slot
        for (int i = 0; i < syncedInventory.Count; i++)
        {
            if (syncedInventory[i] == -1)
            {
                if(currentInventoryIndex == i)
                {
                    UpdateEmptySlotRpc(itemIndex);
                }
                syncedInventory[i] = itemIndex;
                
                return true;
            }
        }

        return false;

    }
    
    [Rpc(SendTo.Owner)]
    private void UpdateEmptySlotRpc(int itemIndex)
    {
        SpawnVisualItemRpc(itemIndex);
    }

    [Rpc(SendTo.Server)]
    private void NewItemSelectedRpc(int inventoryIndex)
    {
        // Always check if item changed, even if index hasn't
        int currentItemIndex = inventoryIndex < syncedInventory.Count ? syncedInventory[inventoryIndex] : -1;

        if (inventoryIndex == lastInventoryIndex && heldObject == GetItemFromIndex(inventoryIndex))
            return; // same slot & same item
        Debug.Log(currentInventoryIndex);
        currentInventoryIndex = inventoryIndex;
        lastInventoryIndex = currentInventoryIndex;
        HighlightNewSlotRpc(currentInventoryIndex);
        if (currentItemIndex != -1)
        {
            Debug.Log("selecting item at slot " + currentInventoryIndex);

            DestroyVisualItemRpc();
            SpawnVisualItemRpc(currentItemIndex);
        }
        else
        {
            DestroyVisualItemRpc();
        }

    }
    [Rpc(SendTo.Owner)]
    private void HighlightNewSlotRpc(int inventoryIndex)
    {
        onNewSlotSelected?.Invoke(inventoryIndex);
    }

    [Rpc(SendTo.Owner)]
    private void SpawnVisualItemRpc(int itemSOIndex)
    {
        GameObject visualItem = Instantiate(itemScriptableObjList.itemListSO[itemSOIndex].visualItemPrefab, itemPosition.position, Quaternion.identity);
        visualItem.transform.SetParent(itemPosition);
        currentVisualItem = visualItem;
        heldObject = itemScriptableObjList.itemListSO[itemSOIndex];
    }

    [Rpc(SendTo.Server)]
    private void DropItemRpc(Vector3 dropPosition, float throwForce)
    {
        int index = syncedInventory[currentInventoryIndex];
        if (index != -1)
        {
            DestroyVisualItemRpc();
            GameObject currentItem = Instantiate(GetItemFromIndex(index).physicalItemPrefab, itemPosition.position, Quaternion.identity);
            currentItem.GetComponent<NetworkObject>().Spawn(true);
            currentItem.GetComponent<InteractableItemBase>().ThrowItem(dropPosition, throwForce);
            syncedInventory[currentInventoryIndex] = -1;
            lastInventoryIndex = -1;
        }
    }

    [Rpc(SendTo.Owner)]
    private void DestroyVisualItemRpc()
    {
        if (currentVisualItem != null)
        {
            Debug.Log("destroying visual item");
            Destroy(currentVisualItem);
            heldObject = null;
        }
    }

    private void OnInventoryChanged(NetworkListEvent<int> changeEvent)
    {
        if (!IsOwner) return;

        switch (changeEvent.Type)
        {
            case NetworkListEvent<int>.EventType.Value:
                Debug.Log($"Slot {changeEvent.Index} updated to item index {changeEvent.Value}");
                if(changeEvent.Value != -1)
                    onAddItem?.Invoke(changeEvent.Index, changeEvent.Value);
                else
                    onDropItem?.Invoke(changeEvent.Index);
                break;
            case NetworkListEvent<int>.EventType.Add:
                Debug.Log($"Item index {changeEvent.Value} added at slot {changeEvent.Index}");
                break;
            case NetworkListEvent<int>.EventType.Clear:
                Debug.Log("Inventory cleared");
                for (int i = 0; i < 4; i++)
                {
                    syncedInventory[currentInventoryIndex] = -1;
                    DestroyVisualItemRpc();
                    lastInventoryIndex = -1;
                }

                break;
        }
    }


    private int GetItemSOIndex(ItemScriptableObject itemScriptableObject)
    {
        return itemScriptableObjList.itemListSO.IndexOf(itemScriptableObject);
    }
    private ItemScriptableObject GetItemFromIndex(int index)
    {
        if (index < 0 || index >= itemScriptableObjList.itemListSO.Count)
            return null;
        return itemScriptableObjList.itemListSO[index];
    }
}
