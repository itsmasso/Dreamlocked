using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Steamworks;
using Unity.Netcode;
using Netcode.Transports.Facepunch;
using UnityEngine.SceneManagement;
using System;



public enum GameState
{
	Lobby,
	GeneratingLevel,
	GameStart,
	GamePlaying,
	GameOver,
	GameBeaten
}

public class GameManager : NetworkSingleton<GameManager>
{

	//events
	public event Action onLobby;
	public event Action onGameStart;
	public event Action onGameStateChanged;
	public event Action onGamePlaying;
	public event Action onLevelGenerate;
	public event Action onNextLevel;
	public NetworkVariable<int> seed = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	public NetworkVariable<GameState> netGameState = new NetworkVariable<GameState>(GameState.Lobby, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	[SerializeField] private ScreenManager screenManager;
	[SerializeField] private LevelLoader levelLoader;
	private const int MAX_DREAM_LAYERS = 10;
	private NetworkVariable<int> currentDreamLayer = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	private float gameOverTimer;
	[SerializeField] private float gameOverScreenDuration = 4f;
	protected override void Awake()
	{
		base.Awake();
		//DontDestroyOnLoad(this.gameObject);


	}

	public override void OnNetworkSpawn()
	{
		if (IsServer)
		{
			NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneLoaded;

			currentDreamLayer.OnValueChanged += (oldValue, newValue) =>
			{
				Debug.Log($"Current Level (dream layer) changed from {oldValue} to {newValue}");
			};

		}


	}

	void Start()
	{

	}
    void Update()
    {
		//debug for skipping levels
        if(Input.GetKeyDown(KeyCode.L))
        {
            OnNextLevel();
        }
    }

    public void OnNextLevel()
	{
		if (!IsServer) return;
		StartCoroutine(TransitionToNextLevel());
		//add respawn here
	}

	private IEnumerator TransitionToNextLevel()
	{
		onNextLevel?.Invoke();
		currentDreamLayer.Value++;
		yield return new WaitForSeconds(0.1f);
		ChangeGameState(GameState.GeneratingLevel);
	}

	public void ChangeGameState(GameState newState)
	{
		if (IsServer)
		{
			onGameStateChanged?.Invoke();
			netGameState.Value = newState;
			switch (netGameState.Value)
			{
				case GameState.Lobby:
					currentDreamLayer.Value = 1;
					Debug.Log("Lobby");
					screenManager.HideGameOverScreen();
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
				case GameState.GameBeaten:
					HandleGameBeatenClientRpc();
					break;
				case GameState.GameOver:
					HandleGameOverClientRpc();
					ShowGameOverScreenClientRpc();
					Debug.Log("Game over");
					gameOverTimer += Time.deltaTime;
					if(gameOverTimer >= gameOverScreenDuration)
					{
						HideGameOverScreenClientRpc();
					    ChangeGameState(GameState.Lobby);
					    gameOverTimer = 0;
					}
					
					break;
				default:
					break;
			}
		}
	}

	[ClientRpc]
	private void HandleLobbyClientRpc()
	{
		onLobby?.Invoke();
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
	private void HandleGameBeatenClientRpc()
	{
	    
	}
	
	[ClientRpc]
	private void HandleGameOverClientRpc()
	{
	    
	}

	[ClientRpc]
	public void ShowSleepLoadingScreenClientRpc()
	{
		screenManager.ShowSleepingLoadingScreen();
	}
	[ClientRpc]
	public void HideSleepLoadingScreenClientRpc()
	{
		screenManager.HideSleepingLoadingScreen();
	}
	
	[ClientRpc]
	public void ShowGameOverScreenClientRpc()
	{
	    screenManager.ShowGameOverScreen();
	}
	
	[ClientRpc]
	public void HideGameOverScreenClientRpc()
	{
	    screenManager.HideGameOverScreen();
	}
	private void SceneLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
	{
		//do something here if needed after scene loads
	}
	
	public LevelLoader GetLevelLoader()
	{
	    return levelLoader;
	}
	public int GetMaxDreamLayer()
	{
		return MAX_DREAM_LAYERS;
	}

	public int GetCurrentDreamLayer()
	{
		return currentDreamLayer.Value;
	}

	public bool IsFinalDreamLayer()
	{
		return currentDreamLayer.Value >= MAX_DREAM_LAYERS;
	}

}