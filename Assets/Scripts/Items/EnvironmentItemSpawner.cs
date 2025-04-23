using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
public class EnvironmentItemSpawner : NetworkBehaviour
{
    [SerializeField] private ItemPlacer itemPlacer;
    [SerializeField] private List<Transform> itemTransforms = new List<Transform>();
    [SerializeField] private List<ItemScriptableObject> potientialItems = new List<ItemScriptableObject>();
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            foreach (Transform itemTransform in itemTransforms)
            {
                itemPlacer.PlaceItem(itemTransform, false, potientialItems);
            }
        }
    }


}
