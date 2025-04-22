using System;
using UnityEngine;
using Unity.Netcode;
using System.Collections;
public class TeddyBearUseable : NetworkBehaviour, IUseableItem<ItemData>
{
    public event Action<ItemData> OnDataChanged;
    public ItemData itemData { private set; get; }
    private int usesRemaining;
    [SerializeField] private float effectRadius;
    [SerializeField] private LayerMask enemyLayer;
    private bool activatedEffect;
    private bool isActive;
    public static event Action<bool> onPlayAnimation;
    void Start()
    {
        if (IsServer)
        {
            activatedEffect = false;
            isActive = false;
        }
    }

    public void InitializeData(ItemData data)
    {
        if (!IsServer) return;
        itemData = data;
        usesRemaining = data.usesRemaining;
    }

    public ItemData GetData()
    {
        return itemData;
    }

    void Update()
    {
        if (IsServer)
        {
            if (isActive && !activatedEffect)
            {
                HandleItemEffect();
            }

        }
    }
    private void HandleItemEffect()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, effectRadius, enemyLayer);
        foreach (Collider col in colliders)
        {
            IAffectedByBear affectedByBear = col.gameObject.GetComponent<IAffectedByBear>();
            if (affectedByBear != null)
            {
                Debug.Log("activating effect");
                affectedByBear.ActivateBearItemEffect();
                PlayDestroyAnimationRpc();
                activatedEffect = true;
            }
        }
    }


    [Rpc(SendTo.Everyone)]
    private void PlayDestroyAnimationRpc()
    {
        if (usesRemaining > 0)
        {
            PlayerInventory playerInventory = GetComponentInParent<PlayerInventory>();
            playerInventory?.RequestServerToDestroyItemRpc();
            usesRemaining--;
            if (usesRemaining <= 0)
                usesRemaining = 0;
        }
    }


    public void UseItem()
    {
        RequestServerToToggleRpc();
    }
    [Rpc(SendTo.Server)]
    private void RequestServerToToggleRpc()
    {
        isActive = !isActive;
        ActivateAnimationRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void ActivateAnimationRpc()
    {
        if(isActive)
        {
            onPlayAnimation(true);
        }else
        {
             onPlayAnimation(false);
        }
    }

}
