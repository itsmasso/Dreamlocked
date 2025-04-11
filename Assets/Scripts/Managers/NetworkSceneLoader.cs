using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections;
public class NetworkSceneLoader : NetworkSingleton<NetworkSceneLoader>
{
    [Header("Scene Names")]
    [SerializeField] private string persistentScene = "PersistScene";
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private string gameLevelScene = "GameScene";
    [SerializeField] private string uiScene = "UIScene";
    [SerializeField] private string defaultActiveScene = "GameScene";

    private Scene currentActiveScene;
    [SerializeField]private bool isProcessingSceneOperation = false;
    private string waitingForSceneName = "";
    private SceneEventType waitingForSceneEventType;
    public override void OnNetworkSpawn()
    {
       
        base.OnNetworkSpawn();
        
        if (IsServer)
        {
        
            NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
            // Optional: Enable active scene synchronization
            NetworkManager.Singleton.SceneManager.ActiveSceneSynchronizationEnabled = true;
            if (!SceneManager.GetSceneByName(persistentScene).isLoaded)
            {
                SceneManager.LoadScene(persistentScene, LoadSceneMode.Single);
                currentActiveScene = SceneManager.GetSceneByName(persistentScene);
            }
            
            
        }
    }

    void Start()
    {
        if(IsServer)
        {
            LoadSceneAdditively("GameScene");
            SetActiveScene("GameScene");
        }
		    
    }
    

    public void LoadSceneAdditively(string sceneName)
    {
        if (!IsServer || isProcessingSceneOperation) return;

        if (SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            Debug.LogWarning($"Scene '{sceneName}' is already loaded. If you want to reload a scene, try calling ReloadSceneAdditively().");
            return;
        }

        var status = NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        if (status != SceneEventProgressStatus.Started)
        {
            Debug.LogError($"Failed to start loading scene '{sceneName}': {status}");
            return;
        }

        waitingForSceneName = sceneName;
        waitingForSceneEventType = SceneEventType.LoadComplete;
        StartCoroutine(WaitForSceneEvent());
    }
    public void ReloadSceneAdditively(string sceneName)
    {
        if (!IsServer || isProcessingSceneOperation) return;
      
        StartCoroutine(ReloadSceneRoutine(sceneName));
    }

    private IEnumerator ReloadSceneRoutine(string sceneName)
    {
        isProcessingSceneOperation = true;
     
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (scene.IsValid() && scene.isLoaded)
        {
            var unloadStatus = NetworkManager.Singleton.SceneManager.UnloadScene(scene);
            if (unloadStatus != SceneEventProgressStatus.Started)
            {
                Debug.LogError($"Failed to start unloading scene '{sceneName}': {unloadStatus}");
                isProcessingSceneOperation = false;
                yield break;
            }

            waitingForSceneName = sceneName;
            waitingForSceneEventType = SceneEventType.UnloadComplete;
            yield return WaitForSceneEvent();
        }

        var loadStatus = NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        if (loadStatus != SceneEventProgressStatus.Started)
        {
            Debug.LogError($"Failed to start loading scene '{sceneName}': {loadStatus}");
            isProcessingSceneOperation = false;
            yield break;
        }

        waitingForSceneName = sceneName;
        waitingForSceneEventType = SceneEventType.LoadComplete;
        
        yield return WaitForSceneEvent();

        isProcessingSceneOperation = false;
    }
    
    [ClientRpc]
    private void RequestSetActiveSceneClientRpc(string sceneName)
    {
        Scene targetScene = SceneManager.GetSceneByName(sceneName);
        if (!targetScene.isLoaded)
        {
            StartCoroutine(WaitForSceneToLoad(sceneName));
            return;
        }

        // Set the scene as active on the client
        SceneManager.SetActiveScene(targetScene);
        currentActiveScene = targetScene;

        Debug.Log($"Active scene set to: {sceneName}");
    }
    public void SetActiveScene(string sceneName)
    {
        if (!IsServer) return;

        Scene targetScene = SceneManager.GetSceneByName(sceneName);
        if (!targetScene.IsValid())
        {
            Debug.LogError($"Scene '{sceneName}' is not valid!");
            return;
        }

        // If scene is not loaded, start the loading process
        if (!targetScene.isLoaded)
        {
            WaitForSceneToLoadClientRpc(sceneName);
            return;
        }

        // If the scene is already loaded, set it as active
        RequestSetActiveSceneClientRpc(sceneName);
        currentActiveScene = targetScene;

        Debug.Log($"Active scene set to: {sceneName}");
    }
    
    [ClientRpc]
    private void WaitForSceneToLoadClientRpc(string sceneName)
    {
        StartCoroutine(WaitForSceneToLoad(sceneName));
    }

    private IEnumerator WaitForSceneToLoad(string sceneName)
    {
        while (true)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (scene.isLoaded)
            {
                SceneManager.SetActiveScene(scene);
                currentActiveScene = scene;
                Debug.Log($"Active scene set to: {sceneName}");
                yield break;
            }

            // Wait a frame before checking again
            yield return null;
        }
    }
    
    public void UnloadSceneAdditively(string sceneName)
    {
        if (!IsServer || isProcessingSceneOperation) return;

        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning($"Scene '{sceneName}' is not valid or already unloaded.");
            return;
        }

        if (scene == currentActiveScene)
        {
            Debug.LogWarning($"Cannot unload currently active scene: '{sceneName}'.");
            return;
        }

        var status = NetworkManager.Singleton.SceneManager.UnloadScene(scene);
        if (status != SceneEventProgressStatus.Started)
        {
            Debug.LogError($"Failed to start unloading scene '{sceneName}': {status}");
            return;
        }

        waitingForSceneName = sceneName;
        waitingForSceneEventType = SceneEventType.UnloadComplete;
        StartCoroutine(WaitForSceneEvent());
    }
    
    private void OnSceneEvent(SceneEvent sceneEvent)
    {
        switch (sceneEvent.SceneEventType)
        {
            case SceneEventType.LoadComplete:
            case SceneEventType.UnloadComplete:
                if (sceneEvent.SceneName == waitingForSceneName &&
                    sceneEvent.SceneEventType == waitingForSceneEventType)
                {
                    Debug.Log($"Scene event complete: {sceneEvent.SceneEventType} for {sceneEvent.SceneName}");
                    waitingForSceneName = "";
                }
                break;

            case SceneEventType.LoadEventCompleted:
            case SceneEventType.UnloadEventCompleted:
                Debug.Log($"{sceneEvent.SceneEventType} for clients: {string.Join(",", sceneEvent.ClientsThatCompleted)}");
                break;
        }
    }

    private IEnumerator WaitForSceneEvent()
    {
        while (!string.IsNullOrEmpty(waitingForSceneName))
        {
            yield return null;
        }
    }
    
    
    public void LoadSceneClientSideOnly(string sceneName)
    {       
        // Check if scene is already loaded
        if (SceneManager.GetSceneByName(sceneName).isLoaded) return;
        
        // Load scene locally on client
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
    }

    public void UnloadSceneClientSideOnly(string sceneName)
    {
        var scene = SceneManager.GetSceneByName(sceneName);
        if (scene.isLoaded)
        {
            SceneManager.UnloadSceneAsync(scene);
        }
    }
    
    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;
        }
    }
}
