using UnityEngine;
using Unity.Netcode;

public class SafePuzzle : NetworkBehaviour, IInteractable
{
    private Transform safeTransform;
    [SerializeField] private GameObject safePrefab;
    [SerializeField] private Animator safeAnimator;
    [SerializeField] private GameObject keyParent;
    private bool keyUnlocked;
    private bool isLocked;
    private bool isOpen;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isOpen = false;
        isLocked = false;
        keyUnlocked = false;
        safeAnimator = GetComponentInChildren<Animator>();
    }

    [ServerRpc(RequireOwnership = false)]
    private void OpenSafeServerRpc()
    {
        OpenSafeClientRpc();
    }

    [ClientRpc]
    private void OpenSafeClientRpc()
    {
        OpenSafe();
    }

    private void OpenSafe()
    {
        Debug.Log("Opening Safe");
        isOpen = true;
        if (safeAnimator != null)
        {
            safeAnimator.SetTrigger("open");
        }
        else
        {
            Debug.Log("Animator Error");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void GetKeyServerRpc()
    {
        GetKeyClientRpc();
    }

    [ClientRpc]
    private void GetKeyClientRpc()
    {
        GetKey();
    }

    private void GetKey()
    {
        Debug.Log("Key Obtained");
        keyParent.SetActive(false);
        keyUnlocked = true;
    }

    public void Interact(NetworkObjectReference playerObject)
    {
        if (!isOpen && !isLocked)
        {
            OpenSafeServerRpc();
        } 
        else if (isOpen && !keyUnlocked)
        {
            GetKeyServerRpc();
        }
    }
}
