using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;

public enum Map
{
    HouseMap
}
public class LevelLoader : NetworkBehaviour
{
    public event Action onLoadMap;
    [Tooltip("Put the scriptable objects in order from easiest to hardest.")]
    public List<HouseMapDifficultySettingsSO> houseMapDifficultySettingList = new List<HouseMapDifficultySettingsSO>();
    public HouseMapDifficultySettingsSO currentHouseMapDifficultySetting;
    private int nextDifficultyCheckpoint;
    private int currentDifficultyIndex;
    void Start()
    {
        if (IsServer)
        {
            GameManager.Instance.ChangeGameState(GameState.GeneratingLevel);
            ResetSettings();
            GameManager.Instance.onLobby += HandleLobbyLoading;
        }
    }
    private IEnumerator WaitUntilGameSceneIsReady(string sceneName, Map level)
    {
        // Check if the scene is already loaded
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (scene.isLoaded)
        {
            // Scene is already loaded, so we can proceed directly
            HandleSceneLoaded(level);
            yield break;  // Skip the coroutine if the scene is already loaded
        }
        // Wait until the additive scene is fully loaded
        while (!SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            yield return null;
        }

        // Now wait an extra frame just in case objects are still initializing
        yield return null;
        HandleSceneLoaded(level);
    }

    private void HandleSceneLoaded(Map map)
    {
        switch (map)
        {
            case Map.HouseMap:
                TryChangeHouseMapDifficultySetting();
                onLoadMap?.Invoke();
                NetworkSceneLoader.Instance.ReloadSceneAdditively("HouseMapLevel");
                break;
        }
    }

    public void LoadHouseMap()
    {
        if (!IsServer) return;
        StartCoroutine(WaitUntilGameSceneIsReady("GameScene", Map.HouseMap));
    }
    
    public void TryChangeHouseMapDifficultySetting()
    {
        if(GameManager.Instance.GetCurrentDreamLayer() == nextDifficultyCheckpoint)
        {
            currentDifficultyIndex++;
            currentHouseMapDifficultySetting = houseMapDifficultySettingList[currentDifficultyIndex];
            nextDifficultyCheckpoint += currentHouseMapDifficultySetting.levelsUntilHarderDifficulty;
        }
    }
    private void HostQuit()
    {
        if (IsHost)
        {
            ClientQuitRpc();
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("MenuScene");
            Debug.Log("Host Quitting and Returning to Lobby");
        }
    }
    [Rpc(SendTo.Everyone)]
    private void ClientQuitRpc()
    {
        if (!IsHost)
        {
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("MenuScene");
            Debug.Log("Client Quitting and Returning to Lobby");
        }
    }
    private void HandleLobbyLoading()
    {
        ResetSettings();
        HostQuit();
    }
    private void ResetSettings()
    {
        Debug.Log("Resetting all Settings");
        //add all resets for all maps here
        currentHouseMapDifficultySetting = houseMapDifficultySettingList[0];
        nextDifficultyCheckpoint = currentHouseMapDifficultySetting.levelsUntilHarderDifficulty;
        currentDifficultyIndex = 0;
    }
    public override void OnDestroy()
    {
        base.OnDestroy();
        if(IsServer) GameManager.Instance.onLobby -= HandleLobbyLoading;
    }
}
