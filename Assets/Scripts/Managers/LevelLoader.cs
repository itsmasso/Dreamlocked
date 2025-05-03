using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using Steamworks;

public enum Map
{
    HouseMap
}
public class LevelLoader : NetworkBehaviour
{
    [Tooltip("Put the scriptable objects in order from easiest to hardest.")]
    public List<HouseMapDifficultySettingsSO> houseMapDifficultySettingList = new List<HouseMapDifficultySettingsSO>();
    private HouseMapDifficultySettingsSO currentHouseMapDifficultySetting;
    public NetworkVariable<int> currentDifficultyIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private int nextDifficultyCheckpoint;
    void Start()
    {
        if (IsServer)
        {
            ResetSettings();
        }
    }

    public void LoadMap(Map map, Action onLoaded = null)
    {
        switch (map)
        {
            case Map.HouseMap:
                TryChangeHouseMapDifficultySetting();
                NetworkSceneLoader.Instance.LoadSceneAdditively("HouseMapLevel", onLoaded);
                break;
        }
    }
    public void ReloadMap(Map map, Action onReloaded = null)
    {
        switch (map)
        {
            case Map.HouseMap:
                TryChangeHouseMapDifficultySetting();
                Scene scene = SceneManager.GetSceneByName("HouseMapLevel");
                if (scene.IsValid() && scene.isLoaded)
                {
                    Debug.Log("[LevelLoader] Scene is loaded, unloading first...");
                    UnloadMap(map, () =>
                    {
                        LoadMap(map, onReloaded);
                    });
                }
                else
                {
                    Debug.Log("[LevelLoader] Scene is not loaded, loading directly...");
                    LoadMap(map, onReloaded);
                }
                break;
        }
    }

    public void UnloadMap(Map map, Action onUnloaded = null)
    {
        switch (map)
        {
            case Map.HouseMap:
                NetworkSceneLoader.Instance.UnloadSceneAdditively("HouseMapLevel", onUnloaded);
                break;
        }
    }

    private void TryChangeHouseMapDifficultySetting()
    {
        if (GameManager.Instance.GetCurrentDreamLayer() == nextDifficultyCheckpoint)
        {
            currentDifficultyIndex.Value++;
            currentHouseMapDifficultySetting = houseMapDifficultySettingList[currentDifficultyIndex.Value];
            nextDifficultyCheckpoint += currentHouseMapDifficultySetting.levelsUntilHarderDifficulty;
        }
    }

    private void ResetSettings()
    {
        Debug.Log("Resetting all Settings");
        //add all resets for all maps here
        currentHouseMapDifficultySetting = houseMapDifficultySettingList[0];
        nextDifficultyCheckpoint = currentHouseMapDifficultySetting.levelsUntilHarderDifficulty;
        currentDifficultyIndex.Value = 0;
    }
    public override void OnDestroy()
    {
        base.OnDestroy();
        if(IsServer) ResetSettings();
    }
}
