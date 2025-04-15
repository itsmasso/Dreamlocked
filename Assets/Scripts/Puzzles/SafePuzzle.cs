using UnityEngine;
using Unity.Netcode;

public class SafePuzzle : NetworkBehaviour, IInteractable
{
    private Transform safeTransform;
    [SerializeField] private GameObject safePrefab;
    [SerializeField] private GameObject safeDoorPrefab;
    [SerializeField] private Animator safeAnimator;
    private bool isLocked;
    private bool isOpen;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isOpen = false;
        isLocked = false;
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

    public void Interact(NetworkObjectReference playerObject)
    {
        if (!isOpen && !isLocked)
        {
            OpenSafeServerRpc();
        }
    }
}
