using UnityEngine;
using Unity.Netcode;
using System;
public class BatteryUseable : NetworkBehaviour, IUseableItem<ItemData>
{
    private PlayerInventory playerInventory;

    public event Action<ItemData> OnDataChanged;
    private ItemData itemData;
    private int usesRemaining;
    [SerializeField] private ItemScriptableObject flashLightScriptable;
    [SerializeField] private float batteryChargeAmount;
    public ItemData GetData()
    {
        return itemData;
    }

    public void InitializeData(ItemData data)
    {
        if (!IsServer) return;
        itemData = data;
        usesRemaining = data.usesRemaining;
        playerInventory = GetComponentInParent<PlayerInventory>();
    }

    [Rpc(SendTo.Server)]
    private void RequestServerToChargeFlashLightRpc()
    {
        if (usesRemaining <= 0) return;

        int lowestBatteryFlashLightUniqueID = -1;
        float lowestCharge = float.MaxValue;

        // Find the flashlight with the lowest charge
        for (int i = 0; i < playerInventory.SyncedInventory.Count; i++)
        {
            ItemData item = playerInventory.SyncedInventory[i];
            if (item.id == flashLightScriptable.id) // match flashlight item type
            {
                if (item.itemCharge < lowestCharge)
                {
                    lowestCharge = item.itemCharge;
                    lowestBatteryFlashLightUniqueID = item.uniqueId;
                }
            }
        }

        if (lowestBatteryFlashLightUniqueID != -1)
        {
            // Get the flashlight item
            ItemData flashlight = playerInventory.GetItemDataByUniqueId(lowestBatteryFlashLightUniqueID);
           
            flashlight.itemCharge += batteryChargeAmount;
            if (flashlight.itemCharge > 100)
                flashlight.itemCharge = 100;
                
            OnDataChanged?.Invoke(flashlight);
            
            usesRemaining--;
           
            if (usesRemaining <= 0)
            {
                usesRemaining = 0;
                // Trigger data update through event
                var data = itemData;
                data.usesRemaining = usesRemaining;
                itemData = data;
                OnDataChanged?.Invoke(itemData);
                playerInventory?.RequestServerToDestroyItemRpc();
            }

        }
    }

    public void UseItem()
    {
         AudioManager.Instance.PlayLocalClientOnly2DSound(AudioManager.Instance.Get2DSound("BatteryChange"), 0.5f, true);
        RequestServerToChargeFlashLightRpc();
    }

}
