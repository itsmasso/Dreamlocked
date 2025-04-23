using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class ItemPlacer : NetworkBehaviour
{
    [SerializeField] private float chanceToSpawnItem = 0.75f;
    [SerializeField] private float itemSpawnRadius = 0.1f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }
    private ItemScriptableObject GetRandomItem(List<ItemScriptableObject> itemList)
    {
        // get the total weight of props
        float totalWeight = 0f;
        foreach (ItemScriptableObject item in itemList)
        {
            totalWeight += item.spawnWeight;
        }

        // pick a random chance value
        float chosenRandomValue = Random.Range(0, totalWeight);
        // keep trying to pick a prop as pity builds up until it guarentees to pick a item
        float cumulativeWeight = 0f;
        foreach (ItemScriptableObject item in itemList)
        {
            cumulativeWeight += item.spawnWeight;
            if (chosenRandomValue <= cumulativeWeight)
            {
                return item;
            }
        }
        Debug.LogWarning("Failed to pick an item by weights! Spawning a random item without weights.");
        return itemList[Random.Range(0, itemList.Count)];
    }
    public NetworkObject PlaceItem(Transform itemTransform, bool isStored, List<ItemScriptableObject> potentialItems)
    {
        if(!IsServer) return null;
        if (Random.value <= chanceToSpawnItem)
        {
            Vector2 randomOffset = Random.insideUnitCircle * itemSpawnRadius;
            Vector3 spawnPosition = itemTransform.position + new Vector3(randomOffset.x, 0f, randomOffset.y);
            ItemScriptableObject randomItemSO = GetRandomItem(potentialItems);
            GameObject itemObj = Instantiate(randomItemSO.droppablePrefab, spawnPosition, itemTransform.rotation);
            NetworkObject itemNetObj = itemObj.GetComponent<NetworkObject>();
            if (itemNetObj != null)
            {
                itemNetObj.Spawn(true);
            }
            if (isStored)
            {
                // Lock rigidbody on spawn
                if (itemObj.TryGetComponent(out Rigidbody rb))
                {
                    rb.isKinematic = true;
                    rb.constraints = RigidbodyConstraints.FreezeAll;
                }
                itemObj.GetComponent<InteractableItemBase>().isStored.Value = true;
            }
            ItemData newItemData = new ItemData
            {
                id = randomItemSO.id,
                itemCharge = randomItemSO.itemCharge,
                usesRemaining = randomItemSO.usesRemaining,
                uniqueId = ItemManager.Instance.GenerateUniqueItemId()
            };
            itemNetObj.TrySetParent(GetComponent<NetworkObject>());
            itemObj.GetComponent<InteractableItemBase>().InitializeItemData(newItemData);
            ItemManager.Instance.RegisterItem(newItemData);
            return itemNetObj;
        }
        return null;
    }


}