using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;
public class DrawerBox : NetworkBehaviour, IInteractable, IHasNetworkChildren
{
    [SerializeField] private float extendAmount;
    [SerializeField] private Transform itemPosition;
    [SerializeField] private float drawerOpenSmoothTime;
    private float defaultZPosition;
    private float targetZPosition;
    private bool isOpen;
    [SerializeField] private List<ItemScriptableObject> itemList = new List<ItemScriptableObject>();
    private GameObject item;
    private InteractableItemBase itemScript;
    [SerializeField] private NetworkVariable<bool> canInteract = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] private float interactCooldown = 1f;
    private float interactCooldownTimer;
    private Coroutine hideRoutine;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            ItemScriptableObject randomItemSO = itemList[Random.Range(0, itemList.Count)];

            GameObject item = Instantiate(randomItemSO.droppablePrefab, itemPosition.position, Quaternion.identity);
            itemScript = item.GetComponent<InteractableItemBase>();

            ItemData newItemData = new ItemData
            {
                id = randomItemSO.id,
                itemCharge = randomItemSO.itemCharge,
                usesRemaining = randomItemSO.usesRemaining
            };

            item.GetComponent<NetworkObject>().Spawn(true); 
            StartCoroutine(InitItemDelayed(item.GetComponent<NetworkObject>(), newItemData));
        }
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
        if (IsServer) defaultZPosition = transform.localPosition.z;

    }

    void Update()
    {
        if (IsServer)
        {
            Vector3 currentLocalPos = transform.localPosition;
            Vector3 targetLocalPos = new Vector3(currentLocalPos.x, currentLocalPos.y, defaultZPosition - targetZPosition);
            transform.localPosition = Vector3.Lerp(currentLocalPos, targetLocalPos, drawerOpenSmoothTime * Time.deltaTime);
            if (item != null) item.transform.position = itemPosition.transform.position;
            if (item != null && item.activeSelf == false)
                item = null;
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
