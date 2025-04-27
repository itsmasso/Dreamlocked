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
            if (drawerBox == null)
                continue;

            var drawerBoxNetObj = drawerBox.GetComponent<NetworkObject>();
            if (drawerBoxNetObj == null)
                continue;

            if (drawerBoxNetObj.IsSpawned)
            {
                var childNetworkHandler = drawerBoxNetObj.GetComponent<IHasNetworkChildren>();
                if (childNetworkHandler != null)
                {
                    childNetworkHandler.DestroyNetworkChildren();
                }

                drawerBoxNetObj.Despawn(true);
            }
            else
            {
                // Optional: clean up in case it exists but isn’t spawned
                Destroy(drawerBox);
            }
        }

        // Optional: clear the list to avoid stale references
        drawerBoxes.RemoveAll(x => x == null);
    }

}
