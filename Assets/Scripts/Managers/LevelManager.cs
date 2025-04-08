using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using System;

public class LevelManager : NetworkBehaviour
{
    public NetworkVariable<int> currentLevel = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public static event Action onNextLevel;
    public override void OnNetworkSpawn()
    {
        if(IsServer)
        {
            currentLevel.Value = 1;
            currentLevel.OnValueChanged += (oldValue, newValue) =>
            {
                Debug.Log($"Current Level changed from {oldValue} to {newValue}");
            };
        }
    }
    public void OnNextLevel()
    {
        if(!IsServer) return;
        onNextLevel?.Invoke();
        GameManager.Instance.ChangeGameState(GameState.GeneratingLevel);
        currentLevel.Value++;
        
        //add respawn here
    }

    void Update()
    {
        if(Input.GetKey(KeyCode.G))
        {
            OnNextLevel();
        }
    }
}
