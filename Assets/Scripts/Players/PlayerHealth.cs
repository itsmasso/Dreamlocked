using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private PlayerScriptable playerScriptable;
    public static event Action onDeath;
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

    // DELETE THIS BEFORE SUBMITTING
    void Update()
    {
        // if (Input.GetKeyDown(KeyCode.P) && IsServer)
        // {
        //     RequestServerTakeDamageRpc(25);
        // }
    }

    public void ResetHealth()
    {
        RequestServerToRestoreHealthRpc(playerScriptable.health);
    }

    [Rpc(SendTo.Server)]
    public void RequestServerToRestoreHealthRpc(int amount)
    {
        if (!IsServer) return;

        int newHealth = Mathf.Clamp(currentHealth.Value + amount, 0, playerScriptable.health);
        currentHealth.Value = newHealth;

        OwnerUpdateHealthRpc(currentHealth.Value);
    }


    [Rpc(SendTo.Server)]
    public void RequestServerTakeDamageRpc(int amount)
    {
        if (!IsServer) return;

        if (currentHealth.Value > 0)
        {
            currentHealth.Value -= amount;
            if (currentHealth.Value <= 0)
            {
                currentHealth.Value = 0;
                Die();
            }

            OwnerUpdateHealthRpc(currentHealth.Value);
        }

    }
    [Rpc(SendTo.Owner)]
    private void OwnerUpdateHealthRpc(int currentHealth)
    {
        onUpdateHealth?.Invoke(currentHealth);
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} | NetworkObjectId: {NetworkObjectId} has died.");
        //PlayerNetworkManager.Instance.UnregisterPlayerClientRpc(GetComponent<NetworkObject>());
        PlayerNetworkManager.Instance.UnregisterPlayerServerRpc(GetComponent<NetworkObject>());
        gameObject.GetComponent<PlayerController>().enabled = false;
        HidePlayerFromAllRpc(GetComponent<NetworkObject>());
        OwnerDiesRpc();
    }

    [Rpc(SendTo.Owner)]
    private void OwnerDiesRpc()
    {
        onDeath?.Invoke();
    }

    [Rpc(SendTo.Everyone)]
    private void HidePlayerFromAllRpc(NetworkObjectReference playerNetworkObjRef)
    {
        if (playerNetworkObjRef.TryGet(out NetworkObject playerNetworkObj))
        {
            playerNetworkObj.GetComponentInChildren<SkinnedMeshRenderer>().enabled = false;
            playerNetworkObj.GetComponent<CapsuleCollider>().enabled = false;
        }
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
    }
}
