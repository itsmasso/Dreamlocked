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
    public static event Action<int> onRemoveSprite;
    public static event Action<int, int, ItemData> onAddSprite;
    public static event Action<int, float> onUpdateChargeBar;
    [SerializeField] private NetworkList<ItemData> syncedInventory = new NetworkList<ItemData>();
    public NetworkList<ItemData> SyncedInventory => syncedInventory;
    //private ItemScriptableObject currentActiveItem;
    [SerializeField] private int currentInventoryIndex;
    [SerializeField] private int lastInventoryIndex = -1;
    private bool isInventoryLocked;

    [Header("Item Properties")]
    [SerializeField] private GameObject itemParent;
    [SerializeField] private Transform itemTransform;
    [SerializeField] private GameObject currentUseableItem;
    [SerializeField] private GameObject currentVisualItem;
    public ItemData currentHeldItemData;
    [Header("Drop Item Properties")]
    [SerializeField] private float throwForce;
    [Header("Animation")]
    [SerializeField] private Animator animator;
    
    void Start()
    {
        isInventoryLocked = false;
        currentInventoryIndex = 0;

        if (IsOwner)
        {
            syncedInventory.OnListChanged += OnInventoryChanged;

            for (int i = 0; i < syncedInventory.Count; i++)
            {
                if (syncedInventory[i].id != -1)
                    onAddSprite?.Invoke(i, syncedInventory[i].id, syncedInventory[i]);
            }
        }

    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

    }

    public void UseItem(InputAction.CallbackContext ctx)
    {
        if (ctx.performed && currentUseableItem != null && !isInventoryLocked)
        {
            if (currentUseableItem.TryGetComponent(out IUseableItem<ItemData> activatableItem))
            {
                activatableItem.UseItem();
            }
        }
    }

    public void OnSlot1(InputAction.CallbackContext ctx)
    {
        if (ctx.performed && !isInventoryLocked)
        {
            RequestServerToSelectNewItemRpc(0);
        }

    }
    public void OnSlot2(InputAction.CallbackContext ctx)
    {
        if (ctx.performed && !isInventoryLocked)
        {
            RequestServerToSelectNewItemRpc(1);
        }

    }
    public void OnSlot3(InputAction.CallbackContext ctx)
    {
        if (ctx.performed && !isInventoryLocked)
        {
            RequestServerToSelectNewItemRpc(2);
        }

    }
    public void OnSlot4(InputAction.CallbackContext ctx)
    {
        if (ctx.performed && !isInventoryLocked)
        {
            RequestServerToSelectNewItemRpc(3);
        }

    }

    public void OnDropItem(InputAction.CallbackContext ctx)
    {
        if (ctx.performed && !isInventoryLocked)
        {
            RequestServerToDropItemRpc(Camera.main.transform.forward, throwForce);
        }
    }
    public void EnsureEmptyInventorySlots(int count)
    {
        while (syncedInventory.Count < count)
        {
            syncedInventory.Add(new ItemData
            {
                id = -1,
                itemCharge = 0,
                usesRemaining = 0,
                uniqueId = -1
            });
        }
    }
    public bool AddItems(ItemData itemData)
    {
        if (!IsServer) return false;
        PickUpAnimationRpc();
        // If current slot is full, add to the first available empty slot
        AudioManager.Instance.Play2DSound(AudioManager.Instance.Get2DSound("ItemPickup"), true);
        for (int i = 0; i < syncedInventory.Count; i++)
        {
            if (syncedInventory[i].id == -1)
            {
                if (currentInventoryIndex == i)
                {
                    SpawnVisualItemRpc(itemData);
                    RequestToSpawnUseableItemRpc(itemData);

                }
                OwnerUpdatesSpriteRpc(i, itemData.id, itemData);

                syncedInventory[i] = itemData;
                return true;
            }
        }

        return false;
    }

    public void RepopulateItem(ItemData itemData)
    {
        if (!IsServer) return;

        for (int i = 0; i < syncedInventory.Count; i++)
        {
            if (syncedInventory[i].id == -1)
            {
                syncedInventory[i] = itemData;
                return;
            }
        }
    }


    [Rpc(SendTo.Owner)]
    private void PickUpAnimationRpc()
    {
        animator.Play("PickUp", animator.GetLayerIndex("HoldingLayer"));
        StartCoroutine(SetHoldingAfterDelay(1f));
    }
    private IEnumerator SetHoldingAfterDelay(float time)
    {
        yield return new WaitForSeconds(time);
        animator.SetBool("isHoldingItem", true);
    }


    [Rpc(SendTo.Owner)]
    private void OwnerUpdatesSpriteRpc(int slotNumber, int id, ItemData itemData)
    {
        onAddSprite?.Invoke(slotNumber, id, itemData);
    }
    private void UpdateItemDataOnChange(ItemData newData)
    {
        if (!IsServer) return;

        for (int i = 0; i < syncedInventory.Count; i++)
        {
            if (syncedInventory[i].uniqueId == newData.uniqueId)
            {
                syncedInventory[i] = newData;
                OwnerUpdatesChargeBarRpc(i, newData);
                if (currentHeldItemData.uniqueId == newData.uniqueId)
                {
                    currentHeldItemData = newData;
                }
                break;
            }
        }
    }
    [Rpc(SendTo.Owner)]
    private void OwnerUpdatesChargeBarRpc(int slotNumber, ItemData newData)
    {
        onUpdateChargeBar?.Invoke(slotNumber, newData.itemCharge);
    }

    public ItemData GetItemDataByUniqueId(int uniqueId)
    {
        foreach (var item in syncedInventory)
        {
            if (item.uniqueId == uniqueId)
                return item;
        }
        return default;
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
            IHasDestroyAnimation hasDestroyAnimation = currentVisualItem.GetComponent<IHasDestroyAnimation>();
            if (hasDestroyAnimation != null)
            {
                hasDestroyAnimation.PlayDestroyAnimation();
            }
            else
            {
                Destroy(currentVisualItem);
            }

            currentVisualItem = null;
        }
    }

    [Rpc(SendTo.Everyone)]
    private void DestroyVisualItemWithoutAnimRpc()
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
                useableItem.OnDataChanged += UpdateItemDataOnChange;
                currentHeldItemData = itemData;
            }
        }

    }

    [Rpc(SendTo.Server)]
    private void RequestServerToDespawnUseableItemRpc()
    {
        if (currentUseableItem != null)
        {
            if (currentUseableItem.TryGetComponent<NetworkObject>(out var netObj))
            {
                if (netObj.IsSpawned)
                {

                    IUseableItem<ItemData> useableItem = currentUseableItem.GetComponent<IUseableItem<ItemData>>();
                    useableItem.OnDataChanged -= UpdateItemDataOnChange;
                    netObj.Despawn(true); //you could optimize this so that we only hide these objects not destroy them
                }
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
        int holdingLayerIndex = animator.GetLayerIndex("HoldingLayer");
        animator.SetLayerWeight(holdingLayerIndex, currentVisualItem != null ? 1f : 0f);
    }

    [Rpc(SendTo.Server)]
    public void RequestServerToSelectNewItemRpc(int inventoryIndex)
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
            DestroyVisualItemWithoutAnimRpc();

            RequestToSpawnUseableItemRpc(selectedItem);
            SpawnVisualItemRpc(selectedItem);
            currentHeldItemData = selectedItem;
            SetHoldItemRpc(true);
        }
        else
        {
            RequestServerToDespawnUseableItemRpc();
            DestroyVisualItemWithoutAnimRpc();
            ItemData emptyItem = new ItemData { id = -1, itemCharge = 0, usesRemaining = 0, uniqueId = -1 };
            currentHeldItemData = emptyItem;
            SetHoldItemRpc(false);
        }

    }

    [Rpc(SendTo.Owner)]
    private void SetHoldItemRpc(bool isHoldingItem)
    {
        animator.SetBool("isHoldingItem", isHoldingItem);
    }

    [Rpc(SendTo.Owner)]
    private void OwnerHighlightsNewSlotRpc(int inventoryIndex)
    {
        onNewSlotSelected?.Invoke(inventoryIndex);
    }


    [Rpc(SendTo.Server)]
    private void RequestServerToDropItemRpc(Vector3 dropDirection, float throwForce)
    {
        ItemData emptyItem = new ItemData { id = -1, itemCharge = 0, usesRemaining = 0, uniqueId = -1 };

        if (syncedInventory[currentInventoryIndex].id != -1)
        {
            // Clean visual + networked object
            RequestServerToDespawnUseableItemRpc();
            DestroyVisualItemWithoutAnimRpc();

            // Spawn drop in world
            var dropPrefab = ItemDatabase.Get(syncedInventory[currentInventoryIndex].id).droppablePrefab;
            var droppedItem = Instantiate(dropPrefab, itemTransform.position, Quaternion.identity);
            droppedItem.GetComponent<NetworkObject>().Spawn(true);
            var itemDataToDrop = syncedInventory[currentInventoryIndex];
            droppedItem.GetComponent<InteractableItemBase>().InitializeItemData(itemDataToDrop);
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
        ItemData emptyItem = new ItemData { id = -1, itemCharge = 0, usesRemaining = 0, uniqueId = -1 };

        if (syncedInventory[currentInventoryIndex].id != -1)
        {
            // Clean visual + networked object
            RequestServerToDespawnUseableItemRpc();
            DestroyVisualItemRpc();

            // Clear inventory
            syncedInventory[currentInventoryIndex] = emptyItem;
            currentUseableItem = null;
            currentHeldItemData = emptyItem;
            lastInventoryIndex = -1;
            UnlockInventoryRpc();
            // Notify client to remove sprite
            OwnerRemovesSpriteRpc(currentInventoryIndex);
        }

    }
    private void OnInventoryChanged(NetworkListEvent<ItemData> changeEvent)
    {
        if (IsOwner)
        {
            if (changeEvent.Type == NetworkListEvent<ItemData>.EventType.Value)
            {
                if (changeEvent.Value.id != -1)
                {
                    onAddSprite?.Invoke(changeEvent.Index, changeEvent.Value.id, changeEvent.Value);
                }
                else
                {
                    onRemoveSprite?.Invoke(changeEvent.Index);
                }
            }
        }
    }

    [Rpc(SendTo.Owner)]
    private void UnlockInventoryRpc()
    {
        isInventoryLocked = false;
    }

    public void LockInventory()
    {
        isInventoryLocked = true;
    }

    public GameObject GetCurrentUsableItem()
    {
        return currentUseableItem;
    }

    public GameObject GetCurrentVisualItem()
    {
        return currentVisualItem;
    }

    public int GetCurrentHeldItemID()
    {
        return currentHeldItemData.id;
    }

    [Rpc(SendTo.Owner)]
    private void OwnerRemovesSpriteRpc(int slot)
    {
        onRemoveSprite?.Invoke(slot);
    }


}
