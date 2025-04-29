using System.Collections;
using System.Linq;
using Steamworks;
using Steamworks.Data;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuHandler : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private TMP_InputField LobbyIDInputField;

    [SerializeField]
    private TextMeshProUGUI LobbyID;

    [SerializeField]
    private GameObject LobbyMenu;
    [SerializeField]
    private GameObject mainMenu;

    [SerializeField]
    private GameObject settingsPanel;
    void Start()
    {
        
    }

    void OnEnable()
    {
        
        SteamMatchmaking.OnLobbyEntered += LobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeft;
    }

    void OnDisable()
    {
        
        SteamMatchmaking.OnLobbyEntered -= LobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeft;
    }

    // private IEnumerator DelayedUIBinding(Scene scene)
    // {
    //     if (scene.name == "MenuScene")
    //     {
    //         // Wait one frame to allow Unity to finish initializing the new scene objects
    //         yield return null;

    //         LobbyIDInputField = GameObject.Find("LobbyInputField")?.GetComponent<TMP_InputField>();
    //         LobbyID = GameObject.Find("LobbyIDText")?.GetComponent<TextMeshProUGUI>();
    //         LobbyMenu = GameObject.Find("LobbyMenu");
    //         mainMenu = GameObject.Find("MainMenu");
    //         settingsPanel = GameObject.Find("SettingsPanel");

    //         Debug.Log("Rebound UI elements in MenuScene");
    //     }
    // }
    private IEnumerator DelayedRefreshLobbyUI(int memberCount)
    {
        yield return new WaitForSeconds(0.1f); // short delay to let LobbyUIManager initialize
        LobbyUIManager.Instance?.RefreshPlayerIcons(memberCount);
    }
    private void LobbyEntered(Lobby lobby)
    {
        string joinCode = lobby.GetData("joinCode");
        LobbyID.text = string.IsNullOrEmpty(joinCode) ? lobby.Id.ToString() : joinCode;

        CheckUI();

        // Delay refreshing icons
        StartCoroutine(DelayedRefreshLobbyUI(lobby.MemberCount));

        StartCoroutine(SendDelayedJoinMessage());
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
    public void HostLobby()
    {
        string joinCode = GenerateRandomCode(6);
        SteamManager.Instance.HostLobby(joinCode);

        LobbyID.text = joinCode;
        CheckUI();

        LobbyUIManager.Instance?.RefreshPlayerIcons(SteamManager.Instance.GetCurrentLobby().MemberCount);
    }
    public void StartGame()
    {
        SteamManager.Instance.StartGameServer();
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
    private void OnLobbyMemberJoined(Lobby lobby, Friend newMember)
    {
        if (LobbySaver.instance.currentLobby.HasValue && lobby.Id == LobbySaver.instance.currentLobby.Value.Id)
        {
            LobbyUIManager.Instance?.RefreshPlayerIcons(lobby.MemberCount);
            Debug.Log($"Player joined lobby. Now {lobby.MemberCount} players.");
        }
    }
    public void JoinLobbyByCode()
    {
        string inputCode = LobbyIDInputField.text.Trim().ToUpper();

        if (string.IsNullOrEmpty(inputCode))
        {
            Debug.LogError("Join code is empty.");
            return;
        }
        SteamManager.Instance.JoinLobbyByCode(inputCode);
        // var lobbies = await SteamMatchmaking.LobbyList.RequestAsync();

        // Debug.Log($"Searching for code: {inputCode}");
        // foreach (var lobby in lobbies)
        // {
        //     string code = lobby.GetData("joinCode");
        //     Debug.Log($"Found lobby: {lobby.Id} with code: {code}");

        //     if (code == inputCode)
        //     {
        //         await lobby.Join();
        //         Debug.Log($"Joining lobby with code: {inputCode}");
        //         return;
        //     }
        // }


    }
    public void LeaveLobby()
    {
        FindFirstObjectByType<ChatManager>()?.ClearChat();
        LobbySaver.instance.currentLobby?.SendChatString($"<b><color=#AAAAAA>[System]</color></b> {SteamClient.Name} has left the lobby.");

        CheckUI();

        if (LobbyUIManager.Instance != null)
        {
            LobbyUIManager.Instance.RefreshPlayerIcons(0);
        }

    }
    public void CopyID()
    {
        TextEditor te = new TextEditor();
        te.text = LobbyID.text;
        te.SelectAll();
        te.Copy();
    }
    private string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        System.Random random = new System.Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private void OnLobbyMemberLeft(Lobby lobby, Friend member)
    {
        if (LobbySaver.instance.currentLobby.HasValue && lobby.Id == LobbySaver.instance.currentLobby.Value.Id)
        {
            LobbyUIManager.Instance?.RefreshPlayerIcons(lobby.MemberCount);
            Debug.Log($"Player left lobby. Now {lobby.MemberCount} players.");
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
