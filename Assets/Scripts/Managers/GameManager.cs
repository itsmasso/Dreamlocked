using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Steamworks;
using Unity.Netcode;
using Netcode.Transports.Facepunch;
using UnityEngine.SceneManagement;
using System;
using Unity.VisualScripting;


public enum GameState
{
	Lobby,
	GeneratingLevel,
	GameStart,
	GamePlaying,
	GameOver,
}

public class GameManager : NetworkSingleton<GameManager>
{
	
	//events
	public event Action onGameStart;
	public event Action onGameStateChanged;
	public event Action onGamePlaying;
	public event Action onLevelGenerate;
	public event Action onNextLevel;
	public NetworkVariable<int> currentLevel = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	public NetworkVariable<int> seed = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	public NetworkVariable<GameState> netGameState = new NetworkVariable<GameState>(GameState.Lobby, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	[SerializeField] private LoadingScreenManager loadingScreenManager;
	[SerializeField] private LevelLoader levelLoader;
	protected override void Awake()
	{
		base.Awake();
		//DontDestroyOnLoad(this.gameObject);
		
		
	}

	public override void OnNetworkSpawn()
	{
		if(IsServer)
		{
			NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneLoaded;
			
			currentLevel.OnValueChanged += (oldValue, newValue) =>
            {
                Debug.Log($"Current Level changed from {oldValue} to {newValue}");
            };
     
		}
		
		
	}

    void Start()
    {
        
    }

    
	public void OnNextLevel()
    {
        if(!IsServer) return;
        StartCoroutine(TransitionToNextLevel());
        //add respawn here
    }
    
    private IEnumerator TransitionToNextLevel()
    {
        onNextLevel?.Invoke();
        currentLevel.Value++;
        yield return new WaitForSeconds(0.1f);
        ChangeGameState(GameState.GeneratingLevel);
    }

	public void ChangeGameState(GameState newState)
	{
		if(IsServer)
		{
			onGameStateChanged?.Invoke();
			netGameState.Value = newState;
			switch(netGameState.Value)
			{
				case GameState.Lobby:
					HandleLobbyClientRpc();
					break;
				case GameState.GeneratingLevel:

					seed.Value = UnityEngine.Random.Range(1, 999999);
					Debug.Log("Current seed:" + seed.Value);
					ShowSleepLoadingScreenClientRpc();
					HandleGenerateLevelClientRpc();
					levelLoader.LoadHouseMap();
					
					break;
				case GameState.GameStart:
					HandleGameStartClientRpc();
					ChangeGameState(GameState.GamePlaying);
					break;
				case GameState.GamePlaying:
					HandleGamePlayingClientRpc();
					break;
				case GameState.GameOver:
					Debug.Log("Game over");
					break;
				default:
					break;
			}
		}
	}
	
	[ClientRpc]
	private void HandleLobbyClientRpc()
	{
	   
	    	
	}
	
	[ClientRpc]
	private void HandleGenerateLevelClientRpc()
	{
		onLevelGenerate?.Invoke();
	   
	}
	
	[ClientRpc]
	private void HandleGameStartClientRpc()
	{
	    onGameStart?.Invoke();
	
	}
	
	[ClientRpc]
	private void HandleGamePlayingClientRpc()
	{
	    onGamePlaying?.Invoke();
	
	}
	
	[ClientRpc]
	public void ShowSleepLoadingScreenClientRpc()
	{
	    loadingScreenManager.ShowSleepingLoadingScreen();
	}
	[ClientRpc]
	public void HideSleepLoadingScreenClientRpc()
	{
	    loadingScreenManager.HideSleepingLoadingScreen();
	}
	private void SceneLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
	{
		//do something here if needed after scene loads
	}

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.G))
        {
            OnNextLevel();
           
        }
    }


}