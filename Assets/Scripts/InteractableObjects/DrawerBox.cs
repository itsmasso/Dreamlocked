using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;
public class DrawerBox : NetworkBehaviour, IInteractable, IHasNetworkChildren
{
    [SerializeField] private float extendAmount;
    [SerializeField] private Transform itemPosition;
    [SerializeField] private float drawerOpenSmoothTime;
    private Vector3 closedPosition;
    private float targetZPosition;
    private bool isOpen;
    [SerializeField] private List<ItemScriptableObject> itemList = new List<ItemScriptableObject>();
    private GameObject item;
    private InteractableItemBase itemScript;
    [SerializeField] private float itemSpawnChance;
    [SerializeField] private NetworkVariable<bool> canInteract = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] private float interactCooldown = 1f;
    private float interactCooldownTimer;
    private Coroutine hideRoutine;
    private Vector3 drawerForward;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            if (Random.value < itemSpawnChance)
            {
                ItemScriptableObject randomItemSO = GetRandomItem(itemList);

                GameObject item = Instantiate(randomItemSO.droppablePrefab, itemPosition.position, Quaternion.identity);
                itemScript = item.GetComponent<InteractableItemBase>();
                item.GetComponent<NetworkObject>().Spawn(true);
                // Lock rigidbody on spawn
                if (item.TryGetComponent(out Rigidbody rb))
                {
                    rb.isKinematic = true;
                    rb.constraints = RigidbodyConstraints.FreezeAll;
                }

                ItemData newItemData = new ItemData
                {
                    id = randomItemSO.id,
                    itemCharge = randomItemSO.itemCharge,
                    usesRemaining = randomItemSO.usesRemaining
                };


                item.GetComponent<NetworkObject>().TrySetParent(GetComponent<NetworkObject>());
                StartCoroutine(InitItemDelayed(item.GetComponent<NetworkObject>(), newItemData));
            }
            else
            {
                item = null;
            }
        }
    }

    private ItemScriptableObject GetRandomItem(List<ItemScriptableObject> itemList)
    {
        // get the total weight of props
        float totalWeight = 0f;
        foreach (ItemScriptableObject item in itemList)
        {
            totalWeight += item.spawnWeight;
        }

        // pick a random chance value
        float chosenRandomValue = Random.Range(0, totalWeight);
        // keep trying to pick a prop as pity builds up until it guarentees to pick a item
        float cumulativeWeight = 0f;
        foreach (ItemScriptableObject item in itemList)
        {
            cumulativeWeight += item.spawnWeight;
            if (chosenRandomValue <= cumulativeWeight)
            {
                return item;
            }
        }
        Debug.LogWarning("Failed to pick an item by weights! Spawning a random item without weights.");
        return itemList[Random.Range(0, itemList.Count)];
    }
    public void SetDrawerDirection(Vector3 forward)
    {
        drawerForward = forward.normalized;
    }

    [Rpc(SendTo.Everyone)]
    private void AllInitItemRpc(NetworkObjectReference networkObjectReference)
    {
        if (networkObjectReference.TryGet(out NetworkObject networkObject))
        {
            item = networkObject.gameObject;
        }
    }
    private IEnumerator InitItemDelayed(NetworkObject netObj, ItemData data)
    {
        yield return null; // wait one frame (lets Netcode finish spawn phase)

        if (netObj.TryGetComponent(out InteractableItemBase itemScript))
        {
            itemScript.InitializeItemData(data);
            itemScript.isStored.Value = true;
            AllInitItemRpc(netObj);
            AllHideItemRpc();
        }
    }


    void Start()
    {
        if (IsServer) closedPosition = transform.position;

    }

    void Update()
    {
        if (IsServer)
        {
            Vector3 targetWorldPos = closedPosition + (drawerForward * targetZPosition);
            transform.position = Vector3.Lerp(transform.position, targetWorldPos, drawerOpenSmoothTime * Time.deltaTime);
            // if (item != null && itemScript != null && itemScript.isStored.Value)
            // {
            //     item.transform.position = itemPosition.position;
            //     item.transform.rotation = itemPosition.rotation;
            // }
            if (item != null && item.activeSelf == false)
            {
                item = null;
            }

            HandleInteractCooldown();
        }

    }

    private void HandleInteractCooldown()
    {
        if (interactCooldownTimer > 0)
        {
            canInteract.Value = false;
            interactCooldownTimer -= Time.deltaTime;
        }

        if (interactCooldownTimer <= 0)
        {
            canInteract.Value = true;
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestServerToToggleDrawerRpc()
    {
        interactCooldownTimer = interactCooldown;
        AllObserveToggleDrawerRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void AllObserveToggleDrawerRpc()
    {
        ToggleDrawer();
    }
    private void ToggleDrawer()
    {
        isOpen = !isOpen;
        if (isOpen)
        {
            if (IsServer && item != null) AllShowItemRpc();
            targetZPosition = extendAmount;
        }
        else
        {
            if (IsServer && item != null)
            {
                if (hideRoutine != null)
                    StopCoroutine(hideRoutine);
                hideRoutine = StartCoroutine(HideDelay());
            }
            targetZPosition = 0f;
        }
    }

    private IEnumerator HideDelay()
    {
        yield return new WaitForSeconds(0.5f);
        AllHideItemRpc();
    }
    [Rpc(SendTo.Everyone)]
    private void AllHideItemRpc()
    {
        if (item != null)
        {
            MeshRenderer meshRenderer = item.GetComponent<MeshRenderer>() != null ? item.GetComponent<MeshRenderer>() : item.GetComponentInChildren<MeshRenderer>();
            Collider col = item.GetComponent<Collider>() != null ? item.GetComponent<Collider>() : item.GetComponentInChildren<Collider>();
            meshRenderer.enabled = false;
            col.enabled = false;
        }
    }
    [Rpc(SendTo.Everyone)]
    private void AllShowItemRpc()
    {
        if (item != null)
        {

            MeshRenderer meshRenderer = item.GetComponent<MeshRenderer>() != null ? item.GetComponent<MeshRenderer>() : item.GetComponentInChildren<MeshRenderer>();
            Collider col = item.GetComponent<Collider>() != null ? item.GetComponent<Collider>() : item.GetComponentInChildren<Collider>();
            meshRenderer.enabled = true;
            col.enabled = true;
            col.isTrigger = true;
        }
    }

    public void Interact(NetworkObjectReference playerObjectRef)
    {
        if (canInteract.Value)
        {
            RequestServerToToggleDrawerRpc();
        }
    }

    public void DestroyNetworkChildren()
    {
        if (item != null)
        {
            item.GetComponent<NetworkObject>().Despawn(true);
        }
    }
}
