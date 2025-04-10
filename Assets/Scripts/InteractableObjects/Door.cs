using UnityEngine;
using Unity.Netcode;
using Pathfinding;
public class Door : NetworkBehaviour, IInteractable
{
    private float targetYRotation;
    [SerializeField] private float smoothTime;
    private float defaultYRotation = 0f;
    [SerializeField] private Transform pivot;
    [SerializeField] private float maxDoorAngle;
    [SerializeField] private NavmeshCut navmeshCut;
    private bool isOpen;
    
    void Start()
    {
        defaultYRotation = transform.eulerAngles.y;
    }

    void Update()
    {
        pivot.rotation = Quaternion.Lerp(pivot.rotation, Quaternion.Euler(0f, defaultYRotation + targetYRotation, 0f), smoothTime * Time.deltaTime);
    }
    
    [ServerRpc]
    private void ToggleDoorServerRpc(Vector3 pos)
    {
        ToggleDoorClientRpc(pos);
    }
    
    [ClientRpc]
    private void ToggleDoorClientRpc(Vector3 pos)
    {
        ToggleDoor(pos);
    }
    private void ToggleDoor(Vector3 pos)
    {
        isOpen = !isOpen;
        if(isOpen)
        {
            Vector3 dir = pos - transform.position;
            targetYRotation = -Mathf.Sign(Vector3.Dot(transform.right, dir)) * maxDoorAngle;
            navmeshCut.enabled = false;
        }else
        {
            navmeshCut.enabled = true;
            targetYRotation = 0f;
        }
    }
    
    public void OpenDoor(Vector3 pos)
    {
        OpenDoorServerRpc(pos);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void OpenDoorServerRpc(Vector3 pos)
    {
        OpenDoorClientRpc(pos);
    }
    
    [ClientRpc]
    public void OpenDoorClientRpc(Vector3 pos)
    {
        if(!isOpen)
        {
            ToggleDoor(pos);
        }
    }
    
    public void CloseDoor(Vector3 pos)
    {
        CloseDoorServerRpc(pos);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void CloseDoorServerRpc(Vector3 pos)
    {
        CloseDoorClientRpc(pos);
    }
    
    [ClientRpc]
    public void CloseDoorClientRpc(Vector3 pos)
    {
        if(isOpen)
        {
            ToggleDoor(pos);
        }
    }

    //for players
    public void Interact(NetworkObjectReference playerObjectRef)
    {
        //Debug.Log("Interact Called From Door.cs");
        if(playerObjectRef.TryGet(out NetworkObject playerObject))
        {
            ToggleDoorServerRpc(playerObject.transform.position);
        }
    }
}
