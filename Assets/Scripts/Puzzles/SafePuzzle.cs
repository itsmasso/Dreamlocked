using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class SafePuzzle : NetworkBehaviour, IInteractable, IHasNetworkChildren
{
    [Header("Item Prefabs")]
    private Animator safeAnimator;
    private GameObject item;
    [SerializeField] private GameObject safeCollider;
    [SerializeField] private Transform itemTransform;
    [SerializeField] private ItemScriptableObject itemScriptableObjectToSpawn;
    private InteractableItemBase itemScript;
    private float interactCooldown = 1f;
    private float interactCooldownTimer;

    [Header("Safe Combinations")]
    private int[] securityCode = new int[4];

    [Header("Network Variables")]
    private NetworkVariable<bool> isOpen = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> canInteract = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        safeAnimator = GetComponentInChildren<Animator>();
        if (IsServer)
        {
            FillArrayWithRandomIntegers(securityCode);
            Debug.Log("Security Code Generated: " + securityCode[0].ToString() + securityCode[1].ToString() + securityCode[2].ToString() + securityCode[3].ToString() );
        }
    }

    // Fills the given array with random positive integers between min and max (inclusive)
    private void FillArrayWithRandomIntegers(int[] array, int minValue = 1, int maxValue = 9)
    {
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = Random.Range(minValue, maxValue + 1); // Random.Range is inclusive on min, exclusive on max, so we add 1
        }
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
           SpawnItem();
        }
    }

    private void SpawnItem()
    {
        item = Instantiate(itemScriptableObjectToSpawn.physicalItemPrefab, itemTransform.position, itemTransform.rotation);
        item.GetComponent<NetworkObject>().Spawn(true);
        itemScript = item.GetComponent<InteractableItemBase>();
        itemScript.isStored.Value = true;
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
        safeCollider.GetComponent<BoxCollider>().enabled = false;
        if (safeAnimator != null)
        {
            safeAnimator.SetTrigger("open");
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

    private bool CheckCode(int[] arr1, int[] arr2)
    {
        Debug.Log("Current Code: " + arr1[0].ToString() + arr1[1].ToString() + arr1[2].ToString() + arr1[3].ToString());
        Debug.Log("Correct Code: " + arr2[0].ToString() + arr2[1].ToString() + arr2[2].ToString() + arr2[3].ToString());
        for (int index = 0; index < arr1.Length; index++)
        {
            if (arr1[index] != arr2[index])
            {
                return false;
            }
        }
        return true;
    }

    public void Interact(NetworkObjectReference playerObject)
    {
        if (!isOpen.Value && canInteract.Value)
        {
            if (CheckCode(KeypadScript.GetEnteredCode(), securityCode))
            {
                //Debug.Log("Code Accepted");
                OpenSafeServerRpc();
            }
            else
            {
                //Debug.Log("Enter Code");
                KeypadScript.AccessKeypad();
            }
        }
    }
}
