// ExitGameManager.cs
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Steamworks;

public class ExitGameManager : NetworkBehaviour
{
    [SerializeField] private string menuSceneName = "MenuScene";
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsClient && !IsHost)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnDisconnectedFromHost;
        }
    }

    private void OnDisconnectedFromHost(ulong clientId)
    {
        if (clientId == NetworkManager.ServerClientId)
        {
            Debug.LogWarning("Disconnected from host. Returning to menu...");
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("MenuScene");
        }
    }
    public void ExitToMenu()
    {
        if (IsHost || IsServer)
        {
            AudioManager.Instance.ClearAllAudio();
            KickClientsAndShutdown();

            // Shutdown Netcode
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene(menuSceneName);
        }
        else if (IsClient)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene(menuSceneName);
        }
    }

    private void KickClientsAndShutdown()
    {
        // Tell clients to go back to menu
        SendClientToMenuClientRpc();

        // Shut down host networking after delay
        StartCoroutine(ShutdownAndLoadMenu());
    }

    [Rpc(SendTo.Everyone)]
    private void SendClientToMenuClientRpc()
    {
        if (!IsHost)
        {
            // This runs on clients only
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene(menuSceneName);
        }
    }

    private System.Collections.IEnumerator ShutdownAndLoadMenu()
    {
        yield return new WaitForSeconds(1.5f); // Allow client RPCs to complete

        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene(menuSceneName);
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnectedFromHost;
    }
}
