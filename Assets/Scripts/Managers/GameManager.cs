using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Steamworks;
using Unity.Netcode;
using Netcode.Transports.Facepunch;
using UnityEngine.SceneManagement;
using TMPro;
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

	// Safe Puzzle Combination
	private int[] securityCodeArray = new int[4];
	private NetworkVariable<int> securityCode = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	[SerializeField] private GameObject BookObject;
	protected override void Awake()
	{
		base.Awake();
		//DontDestroyOnLoad(this.gameObject);
		ItemDatabase.Initialize();
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
		if (IsServer)
		{
			for (int i = 0; i < securityCodeArray.Length; i++)
			{
				securityCodeArray[i] = UnityEngine.Random.Range(1, 10);
			}
			securityCode.Value = (securityCodeArray[0] * 1000) + (securityCodeArray[1] * 100) + (securityCodeArray[2] * 10) + securityCodeArray[3];
			Debug.Log("Security Code Generated: " + securityCode.Value);

			BookObject.transform.Find("Year").GetComponent<TextMeshPro>().SetText(securityCode.Value.ToString());
		}
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
					AllHandlesLobbyRpc();
					break;
				case GameState.GeneratingLevel:

					seed.Value = UnityEngine.Random.Range(1, 999999);
					Debug.Log("Current seed:" + seed.Value);
					ShowSleepLoadingScreenToAllRpc();
					AllHandlesGenerateLevelRpc();
					levelLoader.LoadHouseMap();

					break;
				case GameState.GameStart:
					AllHandlesGameStartRpc();
					ChangeGameState(GameState.GamePlaying);
					break;
				case GameState.GamePlaying:
					AllHandlesGamePlayingRpc();
					break;
				case GameState.GameBeaten:
					AllHandlesGameBeatenRpc();
					break;
				case GameState.GameOver:
					AllHandlesGameOverRpc();
					ShowGameOverScreenToAllRpc();
					Debug.Log("Game over");
					gameOverTimer += Time.deltaTime;
					if(gameOverTimer >= gameOverScreenDuration)
					{
						HideGameOverScreenToAllRpc();
					    ChangeGameState(GameState.Lobby);
					    gameOverTimer = 0;
					}
					
					break;
				default:
					break;
			}
		}
	}

	[Rpc(SendTo.Everyone)]
	private void AllHandlesLobbyRpc()
	{
		onLobby?.Invoke();
	}

	[Rpc(SendTo.Everyone)]
	private void AllHandlesGenerateLevelRpc()
	{
		onLevelGenerate?.Invoke();

	}

	[Rpc(SendTo.Everyone)]
	private void AllHandlesGameStartRpc()
	{
		onGameStart?.Invoke();

	}

	[Rpc(SendTo.Everyone)]
	private void AllHandlesGamePlayingRpc()
	{
		onGamePlaying?.Invoke();

	}
	
	[Rpc(SendTo.Everyone)]
	private void AllHandlesGameBeatenRpc()
	{
	    
	}
	
	[Rpc(SendTo.Everyone)]
	private void AllHandlesGameOverRpc()
	{
	    
	}

	[Rpc(SendTo.Everyone)]
	public void ShowSleepLoadingScreenToAllRpc()
	{
		screenManager.ShowSleepingLoadingScreen();
	}
	[Rpc(SendTo.Everyone)]
	public void HideSleepLoadingScreenToAllRpc()
	{
		screenManager.HideSleepingLoadingScreen();
	}
	
	[Rpc(SendTo.Everyone)]
	public void ShowGameOverScreenToAllRpc()
	{
	    screenManager.ShowGameOverScreen();
	}
	
	[Rpc(SendTo.Everyone)]
	public void HideGameOverScreenToAllRpc()
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

	public NetworkVariable<int> GetSafeCode()
	{
		return securityCode;
	}

}