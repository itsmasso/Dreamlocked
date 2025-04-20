using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class Drawer : NetworkBehaviour, IHasNetworkChildren
{
    public List<Transform> drawerBoxTransforms = new List<Transform>();
    public GameObject drawerPrefab;
    public GameObject drawerBoxPrefab;
    private NetworkObject drawerBox;
    public HouseMapPropPlacer houseMapPropPlacer;
    public List<Transform> propTransforms = new List<Transform>();
    public List<HouseMapPropScriptableObj> potentialProps = new List<HouseMapPropScriptableObj>();
    [SerializeField] private float chanceToSpawnProp = 0.75f;
    [SerializeField] private float propSpawnRadius = 1f;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            foreach (Transform drawerBoxTransform in drawerBoxTransforms)
            {
                GameObject drawerBoxObj = Instantiate(drawerBoxPrefab, drawerBoxTransform.position, drawerBoxTransform.rotation);
                drawerBox = drawerBoxObj.GetComponent<NetworkObject>();
                drawerBox.Spawn(true);
                //drawerBox.TrySetParent(gameObject);
                drawerBoxObj.GetComponent<DrawerBox>().SetDrawerDirection(drawerBoxTransform.forward);
            }

        }
    }

    void Start()
    {
        foreach (Transform propTransforms in propTransforms)
        {
            if (Random.value <= chanceToSpawnProp)
            {
                Randomizer<HouseMapPropScriptableObj> propPicker = new Randomizer<HouseMapPropScriptableObj>(potentialProps);
                Vector2 randomOffset = Random.insideUnitCircle * propSpawnRadius;
                Vector3 spawnPosition = propTransforms.position + new Vector3(randomOffset.x, 0f, randomOffset.y);
                GameObject propObj = Instantiate(propPicker.GetNext().prefab, spawnPosition, propTransforms.rotation);

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

    public void DestroyNetworkChildren()
    {
        foreach (Transform child in transform)
        {
            NetworkObject childNetObj = child.GetComponent<NetworkObject>();
            if (childNetObj != null)
            {
                //Debug.Log($"Child NetworkObject found: {child.gameObject.name}, IsSpawned: {childNetObj.IsSpawned}");
                if (childNetObj.IsSpawned)
                {
                    if (childNetObj.GetComponent<IHasNetworkChildren>() != null)
                        childNetObj.GetComponent<IHasNetworkChildren>().DestroyNetworkChildren();
                    childNetObj.Despawn(true); // Despawn child
                }
            }
        }
    }
}
