using UnityEngine;
using Unity.Netcode;
using TMPro;

public class SafeClueItemSpawn : NetworkBehaviour
{
    [SerializeField] private Transform SafeClueTransform;
    [SerializeField] private ItemScriptableObject SafeClueScriptableObject;
    [SerializeField] private GameObject SafeClueObject;
    private InteractableItemBase safeClueScript;
    private int safeCode;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            // Set the clue of the object BEFORE SPAWNING
            safeCode = GameManager.Instance.GetSafeCode().Value;
            SafeClueObject.transform.Find("Year").GetComponent<TextMeshPro>().SetText(safeCode.ToString());
            
            // Spawn the object
            SafeClueObject = Instantiate(SafeClueScriptableObject.droppablePrefab, SafeClueTransform.position, SafeClueTransform.rotation);
            SafeClueObject.GetComponent<NetworkObject>().Spawn(true);
            safeClueScript = SafeClueObject.GetComponent<InteractableItemBase>();

            ItemData newItemData = new ItemData
            {
                id = SafeClueScriptableObject.id,
                itemCharge = SafeClueScriptableObject.itemCharge,
                usesRemaining = SafeClueScriptableObject.usesRemaining
            };
            safeClueScript.InitializeItemData(newItemData);
            safeClueScript.isStored.Value = true;

            Debug.Log("Safe Clue Spawned");
        }
    }

    public void DestroyNetworkChildren()
    {
        foreach (Transform child in transform)
        {
            NetworkObject childNetObj = child.GetComponent<NetworkObject>();
            if (childNetObj != null)
            {
                if (childNetObj.IsSpawned)
                {
                    if (childNetObj.GetComponent<IHasNetworkChildren>() != null)
                    {
                        childNetObj.GetComponent<IHasNetworkChildren>().DestroyNetworkChildren();
                    }
                    childNetObj.Despawn(true);
                }
            }
        }
    }
}
