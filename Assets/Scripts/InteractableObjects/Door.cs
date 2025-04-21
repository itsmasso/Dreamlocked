using UnityEngine;
using Unity.Netcode;
using Pathfinding;
public class Door : NetworkBehaviour, IInteractable
{
    [SerializeField] private float targetYRotation;
    [SerializeField] private float smoothTime;
    public float defaultYRotation;
    [SerializeField] private Transform pivot;
    [SerializeField] private float maxDoorAngle;
    [SerializeField] private NavmeshCut navmeshCut;
    [SerializeField] private bool isOpen;
    public bool isLocked;
    void Start()
    {
        isOpen = false;
        defaultYRotation = pivot.eulerAngles.y;
    }

    void Update()
    {
        pivot.rotation = Quaternion.Lerp(pivot.rotation, Quaternion.Euler(0f, defaultYRotation + targetYRotation, 0f), smoothTime * Time.deltaTime);
    }

    [Rpc(SendTo.Server)]
    private void RequestServerToToggleDoorRpc(Vector3 pos)
    {
        AllObserveToggleDoorRpc(pos);
    }

    [Rpc(SendTo.Everyone)]
    private void AllObserveToggleDoorRpc(Vector3 pos)
    {
        ToggleDoor(pos);
    }
    private void ToggleDoor(Vector3 pos)
    {

        isOpen = !isOpen;
        if (isOpen)
        {
            Vector3 dir = pos - transform.position;
            targetYRotation = -Mathf.Sign(Vector3.Dot(transform.right, dir)) * maxDoorAngle;
            //navmeshCut.enabled = false;
        }
        else
        {
            //navmeshCut.enabled = true;
            targetYRotation = 0f;
        }
    }

    public void OpenDoor(Vector3 pos)
    {
        RequestServerToOpenDoorRpc(pos);
    }

    [Rpc(SendTo.Server)]
    public void RequestServerToOpenDoorRpc(Vector3 pos)
    {
        AllObserveOpeningDoorRpc(pos);
    }

    [Rpc(SendTo.Everyone)]
    public void AllObserveOpeningDoorRpc(Vector3 pos)
    {
        if (!isOpen)
        {
            ToggleDoor(pos);
        }
    }

    public void CloseDoor(Vector3 pos)
    {
        RequestServerToCloseDoorRpc(pos);
    }

    [Rpc(SendTo.Server)]
    public void RequestServerToCloseDoorRpc(Vector3 pos)
    {
        AllObserveDoorClosingRpc(pos);
    }

    [Rpc(SendTo.Everyone)]
    public void AllObserveDoorClosingRpc(Vector3 pos)
    {
        if (isOpen)
        {
            ToggleDoor(pos);
        }
    }

    //for players
    public void Interact(NetworkObjectReference playerObjectRef)
    {
        if (playerObjectRef.TryGet(out NetworkObject playerObject))
        {
            if(!isLocked)
            {
                RequestServerToToggleDoorRpc(playerObject.transform.position);
            }else
            {
                if (playerObject.GetComponent<PlayerInventory>().GetCurrentVisualItem() != null && 
                    playerObject.GetComponent<PlayerInventory>().GetCurrentVisualItem().name == "KeyVisual(Clone)")
                {
                    Debug.Log("Unlocked Door");
                    isLocked = false;
                    playerObject.GetComponent<PlayerInventory>().RequestServerToDestroyItemRpc();
                }
                else
                {
                    Debug.Log("Door is locked");
                }
            }
        }
    }
}
