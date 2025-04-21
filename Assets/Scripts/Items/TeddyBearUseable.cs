using System;
using UnityEngine;
using Unity.Netcode;
using System.Collections;
public class TeddyBearUseable : NetworkBehaviour, IUseableItem<ItemData>
{
    public static event Action onActivateAnimation;
    public static event Action onDestroyObject;
    public event Action<ItemData> OnDataChanged;
    public ItemData itemData { private set; get; }
    private int usesRemaining;
    private float timeBeforeDestroying;
    [SerializeField] private float effectRadius;
    [SerializeField] private LayerMask enemyLayer;
    void Start()
    {

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
    /// <summary>
    /// It takes several seconds to work
    /// </summary>
    private IEnumerator DelayUntilEffectActivation()
    {
        yield return new WaitForSeconds(timeBeforeDestroying);
        usesRemaining--;
        if (usesRemaining <= 0)
        {
            usesRemaining = 0;
            HandleItemEffect();
            AllDestroyItemRpc();
            PlayerInventory playerInventory = GetComponentInParent<PlayerInventory>();
            if (playerInventory != null)
            {
                playerInventory.RequestServerToDestroyItemRpc();
            }
            else
            {
                Debug.LogError("Failed to get player inventory script!");
            }
        }
        var data = itemData;
        data.usesRemaining = usesRemaining;
        itemData = data;

        OnDataChanged?.Invoke(itemData);
    }
    
    private void HandleItemEffect()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, effectRadius, enemyLayer);
        foreach(Collider col in colliders)
        {
            IAffectedByBear affectedByBear = col.gameObject.GetComponent<IAffectedByBear>();
            if(affectedByBear != null){
                affectedByBear.ActivateBearItemEffect();
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void AllDestroyItemRpc()
    {
        onDestroyObject?.Invoke();

    }
    public void UseItem()
    {
        if (usesRemaining > 0)
        {
            RequestServerToActivateBearRpc();
        }

    }

    [Rpc(SendTo.Everyone)]
    private void AllActivateAnimationRpc()
    {
        onActivateAnimation?.Invoke();
    }
    

    [Rpc(SendTo.Server)]
    private void RequestServerToActivateBearRpc()
    {
        AllActivateAnimationRpc();
        StartCoroutine(DelayUntilEffectActivation());
    }

}
