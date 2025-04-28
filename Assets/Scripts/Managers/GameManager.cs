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
	public event Action onGameLose;
	public event Action onGameWin;
	public event Action onGameStateChanged;
	public event Action onGamePlaying;
	public event Action onLevelGenerate;
	public event Action onNextLevel;
	private Coroutine _gameOverRoutine;
	public NetworkVariable<int> seed = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	public NetworkVariable<GameState> netGameState = new NetworkVariable<GameState>(GameState.Lobby, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	[SerializeField] private ScreenManager screenManager;
	[SerializeField] private LevelLoader levelLoader;
	private const int MAX_DREAM_LAYERS = 3;
	private NetworkVariable<int> currentDreamLayer = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	private float gameOverTimer;
	[SerializeField] private float gameOverScreenDuration = 5f;

	// Safe Puzzle Combination
	private int[] securityCodeArray = new int[4];
	private NetworkVariable<int> securityCode = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	// [SerializeField] private GameObject BookObject;
	// [SerializeField] private GameObject VisualBookObject;
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
			// for (int i = 0; i < securityCodeArray.Length; i++)
			// {
			// 	securityCodeArray[i] = UnityEngine.Random.Range(1, 10);
			// }
			// securityCode.Value = (securityCodeArray[0] * 1000) + (securityCodeArray[1] * 100) + (securityCodeArray[2] * 10) + securityCodeArray[3];
			// Debug.Log("Security Code Generated: " + securityCode.Value);

			//BookObject.transform.Find("Year").GetComponent<TextMeshPro>().SetText(securityCode.Value.ToString());
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

			// Stop any pending coroutine
			if (_gameOverRoutine != null && newState != GameState.GameOver)
			{
				StopCoroutine(_gameOverRoutine);
				_gameOverRoutine = null;
			}

			// Stop any pending coroutine
			if (_gameOverRoutine != null & newState != GameState.GameBeaten)
			{
				StopCoroutine(_gameOverRoutine);
				_gameOverRoutine = null;
			}
			switch (netGameState.Value)
			{
				case GameState.Lobby:
				AudioManager.Instance.Stop2DSound(AudioManager.Instance.Get2DSound("RoomAmbience"), 5f);
					currentDreamLayer.Value = 0;
					Debug.Log("Lobby");

					// POSSIBLE BUG - Might need to call HideGameOverScreenToAllRPC, not screenManager.HideGameOverScreen();
					// Uncomment and comment out HideGameOverScreenToAllRPC() if something breaks related to it
					//screenManager.HideGameOverScreen();
					HideGameOverScreenToAllRpc();
					AllHandlesLobbyRpc();
					break;
				case GameState.GeneratingLevel:
					seed.Value = UnityEngine.Random.Range(1, 999999);
					Debug.Log("Current seed:" + seed.Value);
					GenerateSecurityCode();
					ShowSleepLoadingScreenToAllRpc();
					AllHandlesGenerateLevelRpc();
					levelLoader.LoadHouseMap();

					break;
				case GameState.GameStart:
					AudioManager.Instance.Play2DSound(AudioManager.Instance.Get2DSound("RoomAmbience"), 5f);
					AllHandlesGameStartRpc();
					ChangeGameState(GameState.GamePlaying);
					break;
				case GameState.GamePlaying:
					AllHandlesGamePlayingRpc();
					break;
				case GameState.GameBeaten:
					AllHandlesGameBeatenRpc();
					ShowGameWinScreenToAllRpc();
					Debug.Log("Game Won");

					_gameOverRoutine = StartCoroutine(ReturnToLobbyAfterDelay());
					break;
				case GameState.GameOver:
					AllHandlesGameOverRpc();
					ShowGameOverScreenToAllRpc();
					Debug.Log("Game Lost");

					// Start the timer to return to lobby
					_gameOverRoutine = StartCoroutine(ReturnToLobbyAfterDelay());
					break;
				default:
					break;
			}
		}
	}

	private IEnumerator ReturnToLobbyAfterDelay()
	{
		yield return new WaitForSeconds(gameOverScreenDuration);

		Debug.Log("Returning to Lobby");
		_gameOverRoutine = null;
		ChangeGameState(GameState.Lobby);
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
	    onGameWin?.Invoke();
	}
	
	[Rpc(SendTo.Everyone)]
	private void AllHandlesGameOverRpc()
	{
	    onGameLose?.Invoke();
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
	public void ShowGameWinScreenToAllRpc()
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

	private void GenerateSecurityCode()
	{
		for (int i = 0; i < securityCodeArray.Length; i++)
		{
			securityCodeArray[i] = UnityEngine.Random.Range(1, 10);
		}
		securityCode.Value = (securityCodeArray[0] * 1000) + (securityCodeArray[1] * 100) + (securityCodeArray[2] * 10) + securityCodeArray[3];
		Debug.Log("Security Code Generated: " + securityCode.Value);
	}

}