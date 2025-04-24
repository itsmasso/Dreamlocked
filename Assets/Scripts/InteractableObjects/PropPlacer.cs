using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
public class PropPlacer : NetworkBehaviour, IHasNetworkChildren
{
    public List<Transform> propTransforms = new List<Transform>();
    public List<HouseMapPropScriptableObj> potentialProps = new List<HouseMapPropScriptableObj>();
    [SerializeField] private float chanceToSpawnProp = 0.75f;
    [SerializeField] private float propSpawnRadius = 0.1f;
    private List<GameObject> props = new List<GameObject>();

    public void DestroyNetworkChildren()
    {
        foreach (GameObject prop in props)
        {
            NetworkObject propNetObj = prop.GetComponent<NetworkObject>();
            //Debug.Log($"Child NetworkObject found: {child.gameObject.name}, IsSpawned: {childNetObj.IsSpawned}");
            if (propNetObj != null)
            {
                if (propNetObj.IsSpawned)
                {
                    if (propNetObj.GetComponent<IHasNetworkChildren>() != null)
                        propNetObj.GetComponent<IHasNetworkChildren>().DestroyNetworkChildren();
                    propNetObj.Despawn(true); // Despawn child
                }
            }else
            {
                Destroy(prop);
            }
            
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            PlaceProp();
        }
    }
    public void PlaceProp()
    {
        Randomizer<HouseMapPropScriptableObj> propPicker = new Randomizer<HouseMapPropScriptableObj>(potentialProps);
        foreach (Transform propTransforms in propTransforms)
        {
            if (UnityEngine.Random.value <= chanceToSpawnProp)
            {

                Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * propSpawnRadius;
                if (propTransforms == null)
                {
                    Debug.LogWarning(gameObject + " should not have this script if no prop transforms are given");
                }
                Vector3 spawnPosition = propTransforms.position + new Vector3(randomOffset.x, 0f, randomOffset.y);
                GameObject propObj = Instantiate(propPicker.GetNext().prefab, spawnPosition, propTransforms.rotation);
                props.Add(propObj);
                if (IsServer)
                {
                    NetworkObject propNetobj = propObj.GetComponent<NetworkObject>();
                    if (propNetobj != null)
                    {
                        propNetobj.Spawn(true);
                        propNetobj.TrySetParent(gameObject);
                    }
                }
            }
        }
    }
}
