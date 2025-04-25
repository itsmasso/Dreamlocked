using System;
using System.Collections;
using Steamworks;
using UnityEngine;
using TMPro;
using Steamworks.Data;
using Unity.Netcode;
using Netcode.Transports.Facepunch;
using UnityEngine.SceneManagement;


public class SteamManager : PersistentSingleton<SteamManager>
{
    Lobby currentLobby;

    [SerializeField]
    private TMP_InputField LobbyIDInputField;

    [SerializeField]
    private TextMeshProUGUI LobbyID;

    [SerializeField]
    private GameObject LobbyMenu;
    [SerializeField] private GameObject mainMenu;
    
   
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

    private void LobbyEntered(Lobby lobby)
    {
        LobbySaver.instance.currentLobby = lobby;
        LobbyID.text = lobby.Id.ToString();

        if(NetworkManager.Singleton.IsHost) return;
        NetworkManager.Singleton.GetComponent<FacepunchTransport>().targetSteamId = lobby.Owner.Id;
        NetworkManager.Singleton.StartClient();

        Debug.Log("Lobby Entered");
    }

    private void LobbyCreated(Result result, Lobby lobby)
    {
        if(result == Result.OK)
        {
            lobby.SetPublic();
            lobby.SetJoinable(true);
            NetworkManager.Singleton.StartHost();
            CheckUI();
        }
    }

    private async void GameLobbyJoinRequested(Lobby lobby, SteamId steamID)
    {
        await lobby.Join();
    }
    
     private void CheckUI()
    {
        bool inLobby = LobbySaver.instance.currentLobby != null;

        mainMenu.SetActive(!inLobby);
        LobbyMenu.SetActive(inLobby);
    }

    public async void HostLobby()
    {
        await SteamMatchmaking.CreateLobbyAsync(4);
    }

    public async void JoinLobbyWithID()
    {
        ulong lobbyID;

        // This is just to make sure that no errors happen when a user tries to join a lobby without entering a Lobby ID
        if (LobbyIDInputField.text == "")
        {
            return;
        }
        if(ulong.TryParse(LobbyIDInputField.text, out lobbyID))
        {
            return;
        }
        Lobby [] lobbies = await SteamMatchmaking.LobbyList.WithSlotsAvailable(1).RequestAsync();
        foreach(Lobby lobby in lobbies)
        {
            if(lobby.Id == lobbyID)
            {
                await lobby.Join();
                return;
            }
        }
        
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
        LobbySaver.instance.currentLobby?.Leave();
        LobbySaver.instance.currentLobby = null;
        NetworkManager.Singleton.Shutdown();
        CheckUI();
    }

    public void QuitGame()
    {
        Application.Quit();
    }


}