using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using Netcode.Transports.Facepunch;

public class SteamManager : MonoBehaviour
{
    Lobby currentLobby;

<<<<<<< Updated upstream:Assets/Scripts/SteamManager.cs
    [SerializeField]
    private TMP_InputField LobbyIDInputField;

    [SerializeField]
    private TextMeshProUGUI LobbyID;

    [SerializeField]
    private GameObject MainMenu;

    [SerializeField]
    private GameObject LobbyMenu;
    void OnEnable()
=======
    [Header("UI")]
    [SerializeField] private TMP_InputField LobbyIDInputField;
    [SerializeField] private TextMeshProUGUI LobbyID;
    [SerializeField] private GameObject MainMenu;
    [SerializeField] private GameObject LobbyMenu;
    [SerializeField] private GameObject settingsPanel;
    

    private void OnEnable()
>>>>>>> Stashed changes:Assets/Scripts/Netcode/SteamManager.cs
    {
        SteamMatchmaking.OnLobbyCreated += LobbyCreated;
        SteamMatchmaking.OnLobbyEntered += LobbyEntered;
        SteamFriends.OnGameLobbyJoinRequested += GameLobbyJoinRequested;
    }

    private void OnDisable()
    {
        SteamMatchmaking.OnLobbyCreated -= LobbyCreated;
        SteamMatchmaking.OnLobbyEntered -= LobbyEntered;
        SteamFriends.OnGameLobbyJoinRequested -= GameLobbyJoinRequested;
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
            GameManager.Instance.ChangeGameState(GameState.Lobby);

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


    private void LobbyCreated(Result result, Lobby lobby)
    {
        if (result == Result.OK)
        {
<<<<<<< Updated upstream:Assets/Scripts/SteamManager.cs
            lobby.SetPublic();
            lobby.SetJoinable(true);
            NetworkManager.Singleton.StartHost();
=======
            Debug.Log("Lobby created successfully.");
        }
        else
        {
            Debug.LogError("Failed to create lobby.");
>>>>>>> Stashed changes:Assets/Scripts/Netcode/SteamManager.cs
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


    private async void GameLobbyJoinRequested(Lobby lobby, SteamId steamID)
    {
        await lobby.Join();
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

    public void LeaveLobby()
    {
        FindFirstObjectByType<ChatManager>()?.ClearChat();
        LobbySaver.instance.currentLobby?.SendChatString($"<b><color=#AAAAAA>[System]</color></b> {SteamClient.Name} has left the lobby.");
        LobbySaver.instance.currentLobby?.Leave();
        LobbySaver.instance.currentLobby = null;
        NetworkManager.Singleton.Shutdown();
        CheckUI();
    }

    private void CheckUI()
    {
        bool inLobby = LobbySaver.instance.currentLobby != null;

        MainMenu.SetActive(!inLobby);
        LobbyMenu.SetActive(inLobby);

        // Optional explicit check in case Messenger is disabled separately
        Transform messenger = LobbyMenu.transform.Find("Messenger");
        if (messenger != null)
            messenger.gameObject.SetActive(inLobby);
    }


    private IEnumerator SendDelayedJoinMessage()
    {
        yield return null; // wait 1 frame
        var chatManager = FindFirstObjectByType<ChatManager>();
        LobbySaver.instance.currentLobby?.SendChatString($"<b><color=#AAAAAA>[System]</color></b> {SteamClient.Name} has joined the lobby.");
    }

    public void StartGameServer()
    {
<<<<<<< Updated upstream:Assets/Scripts/SteamManager.cs
        if(NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("PlayerScene", LoadSceneMode.Single);
=======
        if (NetworkManager.Singleton.IsHost)
        {
            Scene menuScene = SceneManager.GetSceneByName("GameScene");
            NetworkManager.Singleton.SceneManager.UnloadScene(menuScene);
            GameManager.Instance.ChangeGameState(GameState.GeneratingLevel);
        }
    }

    public void OpenSettings()
    {
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
    }

    public void OnCloseSettingsButtonPressed()
    {
        if (!KeybindSettingsManager.IsRebindingKey)
        {
            CloseSettings();
>>>>>>> Stashed changes:Assets/Scripts/Netcode/SteamManager.cs
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
