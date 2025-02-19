using UnityEngine;
using Unity.Netcode;
public interface IReactToPlayerGaze
{
    public void ReactToPlayerGaze(NetworkObjectReference playerObjectRef);
}
