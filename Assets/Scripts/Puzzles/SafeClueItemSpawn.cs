using UnityEngine;
using Unity.Netcode;
using TMPro;

public class SafeClueItemSpawn : NetworkBehaviour
{
    private GameObject item;
    [SerializeField] private Transform SafeClueTransform;
    [SerializeField] private ItemScriptableObject SafeClueScriptableObject;
    [SerializeField] private GameObject SafeClueObject;
    [SerializeField] private GameObject VisualSafeClueObject;
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
            VisualSafeClueObject.transform.Find("Year").GetComponent<TextMeshPro>().SetText(safeCode.ToString());
            
            // Spawn the object
            item = Instantiate(SafeClueScriptableObject.droppablePrefab, SafeClueTransform.position, SafeClueTransform.rotation);
            item.GetComponent<NetworkObject>().Spawn(true);
            safeClueScript = item.GetComponent<InteractableItemBase>();

            ItemData newItemData = new ItemData
            {
                id = SafeClueScriptableObject.id,
                itemCharge = SafeClueScriptableObject.itemCharge,
                usesRemaining = SafeClueScriptableObject.usesRemaining
            };
            safeClueScript.InitializeItemData(newItemData);
            safeClueScript.isStored.Value = true;
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
