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
    [SerializeField] private NetworkVariable<bool> canInteract = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] private float interactCooldown = 1f;
    private float interactCooldownTimer;
    private Coroutine hideRoutine;
    private Vector3 drawerForward;
    [SerializeField] private ItemPlacer itemPlacer;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            NetworkObject item = itemPlacer.PlaceItem(itemPosition, true, itemList);
            
            StartCoroutine(InitItemDelayed(item));
        }
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
    private IEnumerator InitItemDelayed(NetworkObjectReference netObjRef)
    {
        yield return null; // wait one frame (lets Netcode finish spawn phase)

        if (netObjRef.TryGet(out NetworkObject netObj))
        {
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
            SkinnedMeshRenderer skinRenderer = item.GetComponent<SkinnedMeshRenderer>() != null ? item.GetComponent<SkinnedMeshRenderer>() : item.GetComponentInChildren<SkinnedMeshRenderer>();
            Collider col = item.GetComponent<Collider>() != null ? item.GetComponent<Collider>() : item.GetComponentInChildren<Collider>();
            if (meshRenderer != null) meshRenderer.enabled = false;
            if (skinRenderer != null) skinRenderer.enabled = false;
            col.enabled = false;
        }
    }
    [Rpc(SendTo.Everyone)]
    private void AllShowItemRpc()
    {
        if (item != null)
        {

            MeshRenderer meshRenderer = item.GetComponent<MeshRenderer>() != null ? item.GetComponent<MeshRenderer>() : item.GetComponentInChildren<MeshRenderer>();
            SkinnedMeshRenderer skinRenderer = item.GetComponent<SkinnedMeshRenderer>() != null ? item.GetComponent<SkinnedMeshRenderer>() : item.GetComponentInChildren<SkinnedMeshRenderer>();
            Collider col = item.GetComponent<Collider>() != null ? item.GetComponent<Collider>() : item.GetComponentInChildren<Collider>();
            if (meshRenderer != null) meshRenderer.enabled = true;
            if (skinRenderer != null) skinRenderer.enabled = true;
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
