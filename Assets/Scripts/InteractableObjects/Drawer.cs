using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
public class Drawer : NetworkBehaviour, IHasNetworkChildren
{
    public List<Transform> drawerBoxTransforms = new List<Transform>();
    public GameObject drawerPrefab;
    public GameObject drawerBoxPrefab;
    private NetworkObject drawerBox;


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
