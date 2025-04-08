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
	
	public NetworkVariable<int> seed = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	private GameState currentState;
	private bool firstGeneration;

	protected override void Awake()
	{
		base.Awake();
		DontDestroyOnLoad(this.gameObject);
	}

	public override void OnNetworkSpawn()
	{
		if(IsServer)
		{
			NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneLoaded;
		}
	}
	

	public void ChangeGameState(GameState newState)
	{
		if(IsServer)
		{
			onGameStateChanged?.Invoke();
			currentState = newState;
			switch(currentState)
			{
				case GameState.Lobby:
					firstGeneration = true;
					break;
				case GameState.GeneratingLevel:

					seed.Value = UnityEngine.Random.Range(1, 999999);
					Debug.Log("Current seed:" + seed.Value);
					if(!firstGeneration)
					{
					    onLevelGenerate?.Invoke();
					    firstGeneration = false;
					}
						
					break;
				case GameState.GameStart:
					onGameStart?.Invoke();
					break;
				case GameState.GamePlaying:
					onGamePlaying?.Invoke();
					break;
				case GameState.GameOver:
					Debug.Log("Game over");
					break;
				default:
					break;
			}
		}
	}
	
	

	private void SceneLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
	{
		ChangeGameState(GameState.GeneratingLevel);
	}
	
}