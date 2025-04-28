using UnityEngine;
using Unity.Netcode;
using System;
public class UnknownPillsUseable : NetworkBehaviour, IUseableItem<ItemData>
{
    public event Action<ItemData> OnDataChanged;
    private ItemData itemData;
    private int usesRemaining;
    private PlayerInventory playerInventory;
    [SerializeField] private int speedBoostAmount;
    [SerializeField] private float speedBoostDuration;
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

    public void UseItem()
    {
        RequestServerToSpeedUpRpc();
        AudioManager.Instance.Play2DSound(AudioManager.Instance.Get2DSound("PopoutPill"), 0f, true);
    }

    [Rpc(SendTo.Server)]
    private void RequestServerToSpeedUpRpc()
    {
        if (usesRemaining > 0)
        {
            OwnerSpeedBoostRpc(speedBoostDuration, speedBoostAmount);
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
    
    [Rpc(SendTo.Owner)]
    private void OwnerSpeedBoostRpc(float duration, int speedBoost)
    {
        GetComponentInParent<PlayerController>().SpeedUp(duration, speedBoost);
    }
}
