using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class SafePuzzle : NetworkBehaviour, IInteractable, IHasNetworkChildren
{
    [Header("Item Prefabs")]
    private Animator safeAnimator;
    private GameObject item;
    [SerializeField] private GameObject safeCollider;
    [SerializeField] private Transform itemTransform;
    [SerializeField] private ItemScriptableObject itemScriptableObject;
    private InteractableItemBase itemScript;
    private float interactCooldown = 1f;
    private float interactCooldownTimer;

    [Header("Safe Combinations")]
    private NetworkVariable<int> securityCode = new NetworkVariable<int>(1987, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Network Variables")]
    private NetworkVariable<bool> isOpen = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> canInteract = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private Sound3DSO openSFX;
    void Start()
    {
        safeAnimator = GetComponentInChildren<Animator>();
        if (IsServer)
        {
            securityCode.Value = GameManager.Instance.GetSafeCode().Value;
            isOpen.Value = false;
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

        item = Instantiate(itemScriptableObject.droppablePrefab, itemTransform.position, itemTransform.rotation);
        item.GetComponent<NetworkObject>().Spawn(true);
        itemScript = item.GetComponent<InteractableItemBase>();

        ItemData newItemData = new ItemData
        {
            id = itemScriptableObject.id,
            itemCharge = itemScriptableObject.itemCharge,
            usesRemaining = itemScriptableObject.usesRemaining
        };
        itemScript.InitializeItemData(newItemData);
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
                    if (childNetObj.GetComponent<IHasNetworkChildren>() != null)
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
        if (IsServer)
        {
            isOpen.Value = true;
        }
        safeCollider.GetComponent<BoxCollider>().enabled = false;
        if (safeAnimator != null)
        {
            Invoke("PlaySFX", 0.5f);
            safeAnimator.SetTrigger("open");
        }
        else
        {
            Debug.Log("Animator Error");
        }
    }
    private void PlaySFX()
    {
        AudioManager.Instance.Play3DSoundServerRpc(AudioManager.Instance.Get3DSoundFromList(openSFX), transform.position, true, 0.8f, 1, 20f, false, GetComponent<NetworkObject>(), 0.5f);
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

    private bool CheckCode(int[] arr1, int correctCode)
    {
        //Debug.Log("Current Code: " + arr1[0].ToString() + arr1[1].ToString() + arr1[2].ToString() + arr1[3].ToString());
        //Debug.Log("Correct Code: " + arr2[0].ToString() + arr2[1].ToString() + arr2[2].ToString() + arr2[3].ToString());
        int enteredCode = (arr1[0] * 1000) + (arr1[1] * 100) + (arr1[2] * 10) + arr1[3];
        if (correctCode == enteredCode)
            AudioManager.Instance.PlayLocalClientOnly2DSound(AudioManager.Instance.Get2DSound("AcessGranted"), 0f, true);
        else
            AudioManager.Instance.PlayLocalClientOnly2DSound(AudioManager.Instance.Get2DSound("AcessDenied"), 0f, true);
        return correctCode == enteredCode;
    }

    public void Interact(NetworkObjectReference playerObject)
    {
        if (!isOpen.Value && canInteract.Value)
        {
            if (CheckCode(KeypadScript.GetEnteredCode(), securityCode.Value))
            {
                Debug.Log("Code Accepted");
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
