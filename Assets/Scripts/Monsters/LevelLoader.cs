using UnityEngine;
using Unity.Netcode;
using  UnityEngine.SceneManagement;
using System;
using System.Collections;

public enum Level
{
    HouseMap
}
public class LevelLoader : NetworkBehaviour
{
    [SerializeField]private string bootStrapSceneName;
    public event Action onLoadMap;
    void Awake()
    {
        
    }
    void Start()
    {
       if(IsServer)
       {
           GameManager.Instance.ChangeGameState(GameState.GeneratingLevel);
            
       }
    }
    private IEnumerator WaitUntilGameSceneIsReady(string sceneName, Level level)
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
    
    private void HandleSceneLoaded(Level level)
{
    switch(level)
    {
        case Level.HouseMap:
            onLoadMap?.Invoke();
            NetworkSceneLoader.Instance.ReloadSceneAdditively("HouseMapLevel");
            break;
    }
}

    public void LoadHouseMap()
    {
        if(!IsServer) return;
        StartCoroutine(WaitUntilGameSceneIsReady("GameScene", Level.HouseMap));
    }
    public override void OnDestroy()
    {
        base.OnDestroy();
        
    }
}
