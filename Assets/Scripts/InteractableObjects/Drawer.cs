using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
public class Drawer : NetworkBehaviour, IHasNetworkChildren
{
    public List<Transform> drawerBoxTransforms = new List<Transform>();
    public GameObject drawerPrefab;
    public GameObject drawerBoxPrefab;
    private List<GameObject> drawerBoxes = new List<GameObject>();


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {

            foreach (Transform drawerBoxTransform in drawerBoxTransforms)
            {
                GameObject drawerBoxObj = Instantiate(drawerBoxPrefab, drawerBoxTransform.position, drawerBoxTransform.rotation);
                drawerBoxes.Add(drawerBoxObj);
                drawerBoxObj.GetComponent<NetworkObject>().Spawn(true);
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

        foreach (GameObject drawerBox in drawerBoxes)
        {
            NetworkObject drawerBoxNetObj = drawerBox.GetComponent<NetworkObject>();
            //Debug.Log($"Child NetworkObject found: {child.gameObject.name}, IsSpawned: {childNetObj.IsSpawned}");
            if (drawerBoxNetObj.IsSpawned)
            {
                if (drawerBoxNetObj.GetComponent<IHasNetworkChildren>() != null)
                    drawerBoxNetObj.GetComponent<IHasNetworkChildren>().DestroyNetworkChildren();
                drawerBoxNetObj.Despawn(true); // Despawn child
            }
        }
    }
}
