using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
public class Cupboard : NetworkBehaviour, IInteractable, IHasNetworkChildren
{
    [SerializeField] private Animator anim;
    private NetworkVariable<bool> isOpen = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] private ItemPlacer itemPlacer;
    private bool canInteract;
    private float interactCooldownTimer;
    private List<GameObject> items = new List<GameObject>();
    [SerializeField] private List<Transform> itemTransforms = new List<Transform>();
    [SerializeField] private List<ItemScriptableObject> potentialItems = new List<ItemScriptableObject>();
    [Header("SFX")]
    [SerializeField] private Sound3DSO cupboardOpenSFX, cupboardCloseSFX;
    void Start()
    {
        if (IsServer)
        {
            isOpen.Value = false;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            foreach (Transform itemTransform in itemTransforms)
            {
                NetworkObject item = itemPlacer.PlaceItem(itemTransform, false, potentialItems);
                if(item != null)
                {
                    items.Add(item.gameObject);
                }
            }
        }
    }

    public void Interact(NetworkObjectReference playerObject)
    {
        if (canInteract)
        {
            RequestToOpenDoorsRpc();
            interactCooldownTimer = 0.1f;
            anim.SetBool("Open", isOpen.Value);
        }

    }
    private void HandleInteractCooldown()
    {
        if (interactCooldownTimer > 0)
        {
            canInteract = false;
            interactCooldownTimer -= Time.deltaTime;
        }

        if (interactCooldownTimer <= 0)
        {
            canInteract = true;
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestToOpenDoorsRpc()
    {
        isOpen.Value = !isOpen.Value;
        if(isOpen.Value)
        {
            AudioManager.Instance.Play3DSoundServerRpc(AudioManager.Instance.Get3DSoundFromList(cupboardOpenSFX), transform.position, true, 1f, 1f, 20f, false, GetComponent<NetworkObject>());
        }else
        {
             AudioManager.Instance.Play3DSoundServerRpc(AudioManager.Instance.Get3DSoundFromList(cupboardCloseSFX), transform.position, true, 1f, 1f, 20f, false, GetComponent<NetworkObject>());
        }
    }
    void Update()
    {
        HandleInteractCooldown();
    }

    public void DestroyNetworkChildren()
    {
        foreach (GameObject item in items)
        {
            if (IsServer && item != null)
            {
                NetworkObject itemNetObj = item.GetComponent<NetworkObject>();
                if(itemNetObj != null)
                {
                    itemNetObj.Despawn(true);
                }
            }
        }
    }
}
