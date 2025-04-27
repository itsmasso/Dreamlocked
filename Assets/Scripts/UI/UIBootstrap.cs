using UnityEngine;
using UnityEngine.SceneManagement;

public class UIBootstrap : MonoBehaviour
{
    public GameObject menuUI;
    public GameObject lobbyUI;
    public GameObject settingsPanel;

    private static UIBootstrap instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject); // Prevent duplicates
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MenuScene")
        {
            menuUI?.SetActive(true);
            lobbyUI?.SetActive(false);
            settingsPanel?.SetActive(false);
        }
        else if (scene.name == "GameScene")
        {
            // Hide everything during gameplay
            menuUI?.SetActive(false);
            lobbyUI?.SetActive(false);
            settingsPanel?.SetActive(false);
        }
    }

    public void ShowLobbyUI(string code)
    {
        menuUI?.SetActive(false);
        lobbyUI?.SetActive(true);
        settingsPanel?.SetActive(false);
    }

    public void ToggleSettings()
    {
        if (settingsPanel != null)
        {
            bool isActive = settingsPanel.activeSelf;
            settingsPanel.SetActive(!isActive);
        }
    }
}
