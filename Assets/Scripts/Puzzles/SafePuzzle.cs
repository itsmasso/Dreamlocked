using UnityEngine;
using Unity.Netcode;

public class SafePuzzle : NetworkBehaviour, IInteractable
{
    private Transform safeTransform;
    [SerializeField] private GameObject safePrefab;
    [SerializeField] private GameObject safeDoorPrefab;
    private NetworkObject safeDoor;
    [SerializeField] private Animation anim;
    private bool isLocked;
    private bool isOpen;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isOpen = false;
        isLocked = false;
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
        anim.Play();
    }

    public void Interact(NetworkObjectReference playerObject)
    {
        if (!isOpen && !isLocked)
        {
            OpenSafeServerRpc();
        }
    }
}
