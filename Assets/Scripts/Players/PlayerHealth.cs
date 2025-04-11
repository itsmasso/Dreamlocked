using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private PlayerScriptable playerScriptable;
    public event Action onDeath;
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public static event Action<int> onTakeDamage;
    private PlayerNetworkManager playerNetworkManager;
    public override void OnNetworkSpawn()
	{
        if(IsServer)
        {
            currentHealth.Value = playerScriptable.health;
        }
        GetPlayerNetworkManager();
	}
	private void GetPlayerNetworkManager()
	{
	    Scene persistScene = SceneManager.GetSceneByName("PersistScene");
			if (persistScene.isLoaded)
			{
				foreach (GameObject rootObj in persistScene.GetRootGameObjects())
				{
					playerNetworkManager = rootObj.GetComponentInChildren<PlayerNetworkManager>(true);
					if (playerNetworkManager != null)
						break;
				}
			}
	}
	
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int amount)
    {
        if (!IsServer) return;  

        currentHealth.Value -= amount;
        if (currentHealth.Value <= 0)
        {
            currentHealth.Value = 0;
            Die();
        }
        
        UpdateHealthClientRpc(currentHealth.Value);
        
    }
    [ClientRpc]
    private void UpdateHealthClientRpc(int currentHealth)
    {
        if(IsOwner)
        {
            onTakeDamage?.Invoke(currentHealth);
        }
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} | NetworkObjectId: {NetworkObjectId} has died.");
        PlayerNetworkManager.Instance.UnregisterPlayerClientRpc(GetComponent<NetworkObject>());
        gameObject.GetComponent<PlayerController>().enabled = false;
        HidePlayerFromPlayersClientRpc(GetComponent<NetworkObject>());
        DieClientRpc();
    }
    
    [ClientRpc]
    private void DieClientRpc()
    {
        if(IsOwner)
        {
            onDeath?.Invoke();
        }
    }
    [ClientRpc]
    private void HidePlayerFromPlayersClientRpc(NetworkObjectReference playerNetworkObjRef)
    {
        if(playerNetworkObjRef.TryGet(out NetworkObject playerNetworkObj))
        {
            playerNetworkObj.GetComponentInChildren<MeshRenderer>().enabled = false;
            playerNetworkObj.GetComponent<CapsuleCollider>().enabled = false;
        }
    }
    
}
