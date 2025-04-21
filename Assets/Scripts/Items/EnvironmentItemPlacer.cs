using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class EnvironmentItemPlacer : NetworkBehaviour
{
    public List<Transform> itemTransforms = new List<Transform>();
    public List<ItemScriptableObject> potentialitems = new List<ItemScriptableObject>();
    [SerializeField] private float chanceToSpawnItem = 0.75f;
    [SerializeField] private float itemSpawnRadius = 0.1f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            PlaceItem();
        }
    }
    public void PlaceItem()
    {
        foreach (Transform itemTransform in itemTransforms)
        {
            if (Random.value <= chanceToSpawnItem)
            {

                Vector2 randomOffset = Random.insideUnitCircle * itemSpawnRadius;
                Vector3 spawnPosition = itemTransform.position + new Vector3(randomOffset.x, 0f, randomOffset.y);
                ItemScriptableObject randomItemSO = potentialitems[Random.Range(0, potentialitems.Count)];
                GameObject itemObj = Instantiate(randomItemSO.droppablePrefab, spawnPosition, itemTransform.rotation);
                NetworkObject itemNetObj = itemObj.GetComponent<NetworkObject>();
                if (itemNetObj != null)
                {
                    itemNetObj.Spawn(true);
                }
                ItemData newItemData = new ItemData
                {
                    id = randomItemSO.id,
                    itemCharge = randomItemSO.itemCharge,
                    usesRemaining = randomItemSO.usesRemaining
                };
                itemObj.GetComponent<InteractableItemBase>().InitializeItemData(newItemData);
            }
        }
    }
}