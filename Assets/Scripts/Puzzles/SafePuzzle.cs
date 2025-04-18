using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class SafePuzzle : NetworkBehaviour, IInteractable, IHasNetworkChildren
{
    private Animator safeAnimator;
    private GameObject key;
    [SerializeField] private GameObject safeCollider;
    [SerializeField] private Transform keyTransform;
    [SerializeField] private ItemScriptableObject itemScriptableObjectToSpawn;
    private InteractableItemBase keyScript;
    private float interactCooldown = 1f;
    private float interactCooldownTimer;

    [Header("Network Variables")]
    private NetworkVariable<bool> isOpen = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isLocked = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> canInteract = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        safeAnimator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (IsServer)
        {
            HandleInteractCooldown();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
           
        }
    }

    private void SpawnKey()
    {
         key = Instantiate(itemScriptableObjectToSpawn.physicalItemPrefab, keyTransform.position, keyTransform.rotation);
            key.GetComponent<NetworkObject>().Spawn(true);
            keyScript = key.GetComponent<InteractableItemBase>();
            keyScript.isStored.Value = true;
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
                    if(childNetObj.GetComponent<IHasNetworkChildren>() != null)
                    {
                        childNetObj.GetComponent<IHasNetworkChildren>().DestroyNetworkChildren();
                    }
                    childNetObj.Despawn(true);
                }
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void OpenSafeServerRpc()
    {
        interactCooldownTimer = interactCooldown;
        OpenSafeClientRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void OpenSafeClientRpc()
    {
        OpenSafe();
    }

    private void OpenSafe()
    {
        Debug.Log("Opening Safe");
        isOpen.Value = true;
        //safeCollider.GetComponent<BoxCollider>().enabled = false;
        if (safeAnimator != null)
        {
            safeAnimator.SetTrigger("open");
            if (IsServer)
            {
                SpawnKey();
            }
        }
        else
        {
            Debug.Log("Animator Error");
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

    public void Interact(NetworkObjectReference playerObject)
    {
        if (!isOpen.Value && !isLocked.Value)
        {
            if(canInteract.Value)
            {
                OpenSafeServerRpc();
            }
        }
    }
}
