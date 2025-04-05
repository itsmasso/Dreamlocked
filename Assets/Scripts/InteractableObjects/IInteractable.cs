using Unity.Netcode;
using UnityEngine;

public interface IInteractable
{
    public void Interact(NetworkObjectReference playerObject);
}
