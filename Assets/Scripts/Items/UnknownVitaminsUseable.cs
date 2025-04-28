using UnityEngine;
using Unity.Netcode;
using System;
public class UnknownVitaminsUseable : NetworkBehaviour, IUseableItem<ItemData>
{
    public event Action<ItemData> OnDataChanged;
    private ItemData itemData;
    private int usesRemaining;
    private PlayerHealth playerHealth;
    private PlayerInventory playerInventory;
    [SerializeField] private int healAmount;
    public ItemData GetData()
    {
        return itemData;
    }

    public void InitializeData(ItemData data)
    {
        if (!IsServer) return;
        itemData = data;
        usesRemaining = data.usesRemaining;
        playerHealth = GetComponentInParent<PlayerHealth>();
        playerInventory = GetComponentInParent<PlayerInventory>();
    }

    public void UseItem()
    {
        RequestServerToHealRpc();
        AudioManager.Instance.Play2DSound(AudioManager.Instance.Get2DSound("VitaminSFX"), 0f, true);
    }

    [Rpc(SendTo.Server)]
    private void RequestServerToHealRpc()
    {
        if (usesRemaining > 0)
        {
            playerHealth.RequestServerToRestoreHealthRpc(healAmount);
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

}
