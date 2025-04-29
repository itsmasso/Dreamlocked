using UnityEngine;
using Unity.Netcode;
using TMPro;

public class SafeClueItemSpawn : NetworkBehaviour, IHasNetworkChildren
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
            SetBookTextClientRpc();
            
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
        SafeClueObject.transform.Find("Year").GetComponent<TextMeshPro>().SetText(safeCode.ToString());
    }

    public void DestroyNetworkChildren()
    {
       if (item != null)
        {
            item.GetComponent<NetworkObject>().Despawn(true);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void SetBookTextClientRpc()
    {
        safeCode = GameManager.Instance.GetSafeCode().Value;
        SafeClueObject.transform.Find("Year").GetComponent<TextMeshPro>().SetText(safeCode.ToString());
        VisualSafeClueObject.transform.Find("Year").GetComponent<TextMeshPro>().SetText(safeCode.ToString());
        Debug.Log("Setting Safe Clue: " + safeCode);
    }
}
