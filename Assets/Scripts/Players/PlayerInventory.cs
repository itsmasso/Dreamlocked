using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInventory : NetworkBehaviour
{
    [SerializeField] private ItemListScriptableObject itemScriptableObjList;
    [SerializeField] private List<ItemScriptableObject> inventoryList = new List<ItemScriptableObject>();
    //private ItemScriptableObject currentActiveItem;
    [SerializeField]private int currentInventoryIndex;
    [SerializeField]private int lastInventoryIndex = -1;

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

    public void OnSlot1(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            currentInventoryIndex = 0;
            NewItemSelectedServerRpc();
        }

    }
    public void OnSlot2(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            currentInventoryIndex = 1;
            NewItemSelectedServerRpc();
        }

    }
    public void OnSlot3(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            currentInventoryIndex = 2;
            NewItemSelectedServerRpc();
        }

    }
    public void OnSlot4(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            currentInventoryIndex = 3;
            NewItemSelectedServerRpc();
        }

    }

    public void OnDropItem(InputAction.CallbackContext ctx)
    {
        if (ctx.performed && heldObject != null)
        {
            DropItemServerRpc(Camera.main.transform.forward, throwForce);
        }
    }
    public bool AddItems(ItemScriptableObject itemScriptableObject)
    {
        if (!IsServer) return false;

        // Expand inventory list to match the number of slots if needed
        while (inventoryList.Count < 4)
        {
            inventoryList.Add(null);
        }

        // If current slot is full, add to the first available empty slot
        for (int i = 0; i < inventoryList.Count; i++)
        {
            if (inventoryList[i] == null)
            {
                inventoryList[i] = itemScriptableObject;
                NewItemSelectedServerRpc();
                return true;
            }
        }

        return false;

    }

    [ServerRpc]
    private void NewItemSelectedServerRpc()
    {
        // Always check if item changed, even if index hasn't
        ItemScriptableObject currentItemSO = currentInventoryIndex < inventoryList.Count ? inventoryList[currentInventoryIndex] : null;

        if (currentInventoryIndex == lastInventoryIndex && heldObject == currentItemSO)
            return; // same slot & same item

        lastInventoryIndex = currentInventoryIndex;

        if (currentItemSO != null)
        {
            Debug.Log("selecting item at slot " + currentInventoryIndex);
            DestroyVisualItemClientRpc();
            SpawnVisualItemClientRpc(GetItemSOIndex(currentItemSO));
        }
        else
        {
            DestroyVisualItemClientRpc();
        }


    }
    [ClientRpc]
    private void SpawnVisualItemClientRpc(int itemSOIndex)
    {
        GameObject visualItem = Instantiate(itemScriptableObjList.itemListSO[itemSOIndex].visualItemPrefab, itemPosition.position, Quaternion.identity);
        visualItem.transform.SetParent(itemPosition);
        currentVisualItem = visualItem;
        heldObject = itemScriptableObjList.itemListSO[itemSOIndex];
        Debug.Log("spawning vsual item");
    }

    [ServerRpc]
    private void DropItemServerRpc(Vector3 dropPosition, float throwForce)
    {
        if (inventoryList[currentInventoryIndex] != null)
        {
            GameObject currentItem = Instantiate(heldObject.physicalItemPrefab, itemPosition.position, Quaternion.identity);
            currentItem.GetComponent<NetworkObject>().Spawn(true);
            currentItem.GetComponent<InteractableItemBase>().ThrowItem(dropPosition, throwForce);
            inventoryList[currentInventoryIndex] = null;
            DestroyVisualItemClientRpc();
            lastInventoryIndex = -1;
        }
    }

    [ClientRpc]
    private void DestroyVisualItemClientRpc()
    {
        if (currentVisualItem != null)
        {
            Debug.Log("destroying item");
            Destroy(currentVisualItem);
            heldObject = null;
        }
    }


    private int GetItemSOIndex(ItemScriptableObject itemScriptableObject)
    {
        return itemScriptableObjList.itemListSO.IndexOf(itemScriptableObject);
    }
}
