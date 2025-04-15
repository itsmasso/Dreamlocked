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
    public static event Action<int> onUpdateHealth;
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = playerScriptable.health;
        }

    }
    
    public void ResetHealth()
    {
        RestoreHealthServerRpc(playerScriptable.health);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void RestoreHealthServerRpc(int amount)
    {
        if (!IsServer) return;

        int newHealth = Mathf.Clamp(currentHealth.Value + amount, 0, playerScriptable.health);
        currentHealth.Value = newHealth;

        UpdateHealthClientRpc(currentHealth.Value);
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
        if (IsOwner)
        {
            onUpdateHealth?.Invoke(currentHealth);
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
        if (IsOwner)
        {
            onDeath?.Invoke();
        }
    }
    [ClientRpc]
    private void HidePlayerFromPlayersClientRpc(NetworkObjectReference playerNetworkObjRef)
    {
        if (playerNetworkObjRef.TryGet(out NetworkObject playerNetworkObj))
        {
            playerNetworkObj.GetComponentInChildren<MeshRenderer>().enabled = false;
            playerNetworkObj.GetComponent<CapsuleCollider>().enabled = false;
        }
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
    }
}
