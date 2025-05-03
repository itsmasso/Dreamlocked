using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections;
public class NetworkSceneLoader : NetworkSingleton<NetworkSceneLoader>
{
    private Scene currentActiveScene;
    [SerializeField] private bool isProcessingSceneOperation = false;
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
        }
    }

    public void LoadSceneAdditively(string sceneName, System.Action onComplete = null)
    {
        if (!IsServer || isProcessingSceneOperation) return;

        if (SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            Debug.LogWarning($"Scene '{sceneName}' is already loaded.");
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
        StartCoroutine(WaitForSceneEvent(onComplete));
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
            AllWaitForSceneLoadRpc(sceneName);
            return;
        }

        // If the scene is already loaded, set it as active
        AllSetActiveSceneRpc(sceneName);
        currentActiveScene = targetScene;

        Debug.Log($"Active scene set to: {sceneName}");
    }

    private IEnumerator WaitForSceneEventThen(System.Action callback)
    {
        while (!string.IsNullOrEmpty(waitingForSceneName))
            yield return null;

        callback?.Invoke();
    }

    [Rpc(SendTo.Everyone)]
    private void AllSetActiveSceneRpc(string sceneName)
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

    [Rpc(SendTo.Everyone)]
    private void AllWaitForSceneLoadRpc(string sceneName)
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
                //Debug.Log($"Active scene set to: {sceneName}");
                yield break;
            }

            // Wait a frame before checking again
            yield return null;
        }
    }

    public void UnloadSceneAdditively(string sceneName, System.Action onComplete = null)
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
        StartCoroutine(WaitForSceneEventThen(onComplete));
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
                Debug.Log($"LoadEventCompleted: Clients loaded scene {sceneEvent.SceneName}: {string.Join(",", sceneEvent.ClientsThatCompleted)}");

                if (sceneEvent.ClientsThatTimedOut != null && sceneEvent.ClientsThatTimedOut.Count > 0)
                {
                    Debug.LogWarning($"Clients failed to load scene {sceneEvent.SceneName}: {string.Join(",", sceneEvent.ClientsThatTimedOut)}");
                }

                if (sceneEvent.ClientsThatCompleted.Count == NetworkManager.Singleton.ConnectedClientsList.Count)
                {
                    Debug.Log($"All clients successfully loaded {sceneEvent.SceneName}.");
                }
                break;

            case SceneEventType.UnloadEventCompleted:
                Debug.Log($"UnloadEventCompleted: Clients unloaded scene {sceneEvent.SceneName}: {string.Join(",", sceneEvent.ClientsThatCompleted)}");

                if (sceneEvent.ClientsThatTimedOut != null && sceneEvent.ClientsThatTimedOut.Count > 0)
                {
                    Debug.LogWarning($"Clients failed to unload scene {sceneEvent.SceneName}: {string.Join(",", sceneEvent.ClientsThatTimedOut)}");
                }
                break;
        }
    }

    private IEnumerator WaitForSceneEvent(System.Action callback)
    {
        while (!string.IsNullOrEmpty(waitingForSceneName))
        {
            yield return null;
        }
        callback?.Invoke();
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
