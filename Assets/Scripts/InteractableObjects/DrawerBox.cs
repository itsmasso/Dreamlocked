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
    [SerializeField] private List<ItemScriptableObject> itemScriptableObjectsList = new List<ItemScriptableObject>();
    private ItemScriptableObject itemScriptableObjectToSpawn;
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
            itemScriptableObjectToSpawn = itemScriptableObjectsList[Random.Range(0, itemScriptableObjectsList.Count)];
            item = Instantiate(itemScriptableObjectToSpawn.physicalItemPrefab, itemPosition.position, itemPosition.rotation);
            item.GetComponent<NetworkObject>().Spawn(true);
            AllInitItemRpc(item.GetComponent<NetworkObject>());
            itemScript = item.GetComponent<InteractableItemBase>();
            itemScript.isStored.Value = true;
            AllHideItemRpc();
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
            var rb = item.GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            item.GetComponent<MeshRenderer>().enabled = false;
            item.GetComponent<Collider>().enabled = false;
        }
    }
    [Rpc(SendTo.Everyone)]
    private void AllShowItemRpc()
    {
        if (item != null)
        {
            var rb = item.GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            item.GetComponent<MeshRenderer>().enabled = true;
            item.GetComponent<Collider>().enabled = true;
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
        if(item != null)
        {
            item.GetComponent<NetworkObject>().Despawn(true);
        }
    }
}
