// ExitGameManager.cs
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Steamworks;

public class ExitGameManager : NetworkBehaviour
{
    [SerializeField] private string menuSceneName = "MenuScene"; 

    public void ExitToMenu()
    {
        if (IsHost || IsServer)
        {
            AudioManager.Instance.ClearAllAudio();
            KickClientsAndShutdown();

            // // Shutdown Netcode
            // Cursor.visible = true;
		    // Cursor.lockState = CursorLockMode.None;
            // NetworkManager.Singleton.Shutdown();
            // SceneManager.LoadScene(menuSceneName);
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
        // StartCoroutine(ShutdownAndLoadMenu());
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

    // private System.Collections.IEnumerator ShutdownAndLoadMenu()
    // {
    //     yield return new WaitForSeconds(0.5f); // Allow client RPCs to complete

    //     NetworkManager.Singleton.Shutdown();
    //     SceneManager.LoadScene(menuSceneName);
    // }
}
