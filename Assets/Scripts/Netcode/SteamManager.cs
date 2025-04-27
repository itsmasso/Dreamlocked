using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using Netcode.Transports.Facepunch;


public class SteamManager : Singleton<SteamManager>
{
    Lobby currentLobby;
    
    [Header("UI")]
    [SerializeField]
    private TMP_InputField LobbyIDInputField;

    [SerializeField]
    private TextMeshProUGUI LobbyID;

    [SerializeField]
    private GameObject LobbyMenu;
    [SerializeField] private GameObject mainMenu;

    [SerializeField] private GameObject settingsPanel;
    
   
    void OnEnable()
    {
        SteamMatchmaking.OnLobbyCreated += LobbyCreated;
        SteamMatchmaking.OnLobbyEntered += LobbyEntered;
        SteamFriends.OnGameLobbyJoinRequested += GameLobbyJoinRequested;

    }

    void OnDisable()
    {
        SteamMatchmaking.OnLobbyCreated -= LobbyCreated;
        SteamMatchmaking.OnLobbyEntered -= LobbyEntered;
        SteamFriends.OnGameLobbyJoinRequested -= GameLobbyJoinRequested;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(DelayedUIBinding(scene));
    }

    private IEnumerator DelayedUIBinding(Scene scene)
    {
        if (scene.name == "MenuScene")
        {
            // Wait one frame to allow Unity to finish initializing the new scene objects
            yield return null;

            LobbyIDInputField = GameObject.Find("LobbyInputField")?.GetComponent<TMP_InputField>();
            LobbyID = GameObject.Find("LobbyIDText")?.GetComponent<TextMeshProUGUI>();
            LobbyMenu = GameObject.Find("LobbyMenu");
            mainMenu = GameObject.Find("MainMenu");
            settingsPanel = GameObject.Find("SettingsPanel");

            Debug.Log("Rebound UI elements in MenuScene");
        }
    }


    private void LobbyEntered(Lobby lobby)
    {
        LobbySaver.instance.currentLobby = lobby;

        string joinCode = lobby.GetData("joinCode");
        LobbyID.text = string.IsNullOrEmpty(joinCode) ? lobby.Id.ToString() : joinCode;

        CheckUI();

        if (!NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.GetComponent<FacepunchTransport>().targetSteamId = lobby.Owner.Id;
            NetworkManager.Singleton.StartClient();
        }

        // Wait a frame to ensure ChatManager is present
        StartCoroutine(SendDelayedJoinMessage());
    }


    private void LobbyCreated(Result result, Lobby lobby)
    {
        if (result == Result.OK)
        {
            Debug.Log("Lobby created successfully.");
        }
        else
        {
            Debug.LogError("Failed to create lobby.");
        }
    }

    private async void GameLobbyJoinRequested(Lobby lobby, SteamId steamID)
    {
        await lobby.Join();
    }
    
    private void CheckUI()
    {

        if (mainMenu == null || LobbyMenu == null) return;

        bool inLobby = LobbySaver.instance.currentLobby != null;

        mainMenu.SetActive(!inLobby);
        LobbyMenu.SetActive(inLobby);

        // explicit check in case Messenger is disabled separately
        Transform messenger = LobbyMenu.transform.Find("Messenger");
        if (messenger != null)
            messenger.gameObject.SetActive(inLobby);
    }

    public async void HostLobby()
    {
        Lobby? maybeLobby = await SteamMatchmaking.CreateLobbyAsync(4);
        if (maybeLobby.HasValue)
        {
            Lobby lobby = maybeLobby.Value;

            string joinCode = GenerateRandomCode(6);
            lobby.SetPublic();
            lobby.SetJoinable(true);
            lobby.SetData("joinCode", joinCode);

            LobbySaver.instance.currentLobby = lobby;
            NetworkManager.Singleton.StartHost();

            LobbyID.text = joinCode;
            CheckUI();

            Debug.Log($"Hosting lobby with code: {joinCode}");
        }
        else
        {
            Debug.LogError("Failed to create lobby.");
        }
    }

    public async void JoinLobbyByCode()
    {
        string inputCode = LobbyIDInputField.text.Trim().ToUpper();

        if (string.IsNullOrEmpty(inputCode))
        {
            Debug.LogError("Join code is empty.");
            return;
        }

        var lobbies = await SteamMatchmaking.LobbyList.RequestAsync();

        Debug.Log($"Searching for code: {inputCode}");
        foreach (var lobby in lobbies)
        {
            string code = lobby.GetData("joinCode");
            Debug.Log($"Found lobby: {lobby.Id} with code: {code}");

            if (code == inputCode)
            {
                await lobby.Join();
                Debug.Log($"Joining lobby with code: {inputCode}");
                return;
            }
        }

        Debug.LogError($"No lobby found with code: {inputCode}");
    }

    

    private string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        System.Random random = new System.Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }


    public void CopyID()
    {
        TextEditor te = new TextEditor();
        te.text = LobbyID.text;
        te.SelectAll();
        te.Copy();
    }
    public void StartGameServer()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("PersistScene", LoadSceneMode.Single);
        }
    }
    public void LeaveLobby()
    {
        FindFirstObjectByType<ChatManager>()?.ClearChat();
        LobbySaver.instance.currentLobby?.SendChatString($"<b><color=#AAAAAA>[System]</color></b> {SteamClient.Name} has left the lobby.");
        LobbySaver.instance.currentLobby?.Leave();
        LobbySaver.instance.currentLobby = null;
        NetworkManager.Singleton.Shutdown();
        CheckUI();
    }

    private IEnumerator SendDelayedJoinMessage()
    {
        yield return null; // wait 1 frame
        var chatManager = FindFirstObjectByType<ChatManager>();
        LobbySaver.instance.currentLobby?.SendChatString($"<b><color=#AAAAAA>[System]</color></b> {SteamClient.Name} has joined the lobby.");
    }
    public void OpenSettings()
    {
        // Load keybinds when opening settings
        var keybindManager = FindFirstObjectByType<KeybindSettingsManager>();
        keybindManager?.LoadBindings();
        
        mainMenu.SetActive(false);         // hide main menu
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        // Save any rebinds before hiding the panel
        var keybindManager = FindFirstObjectByType<KeybindSettingsManager>();
        if (keybindManager != null)
        {
            keybindManager.SaveBindings();
        }

        settingsPanel.SetActive(false);
        mainMenu.SetActive(true);         // open main menu
    }

    public void OnCloseSettingsButtonPressed()
    {
        if (!KeybindSettingsManager.IsRebindingKey)
        {
            CloseSettings();
        }
    }

    public void QuitGame()
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }

}