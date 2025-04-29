using UnityEngine;
using Unity.Netcode;

public class PersistentNetworkManager : MonoBehaviour
{
    private void Awake()
    {
        // If there's already a NetworkManager singleton that isn't us, destroy the duplicate
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.gameObject != gameObject)
        {
            Destroy(gameObject);
            return;
        }   

        // Otherwise, make this object live across scene loads
        DontDestroyOnLoad(gameObject);
    }
}

