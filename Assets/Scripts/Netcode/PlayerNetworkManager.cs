using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Unity.Netcode.Components;
using System;
using System.Linq;

public class PlayerNetworkManager : NetworkSingleton<PlayerNetworkManager>
{
	public const int MAX_PLAYERS = 4;
	[SerializeField] private GameObject playerPrefab;
	public List<NetworkObject> alivePlayers = new List<NetworkObject>();
	public NetworkVariable<int> spawnIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone);
	public NetworkVariable<bool> spawnedPlayers = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone);
	public static event Action onRespawnPlayer;
	[SerializeField] private LoadingScreenManager loadingScreenManager;

	void Start()
	{
		if (IsServer)
		{
			spawnedPlayers.Value = false;
			GameManager.Instance.onGameStart += RespawnPlayers;

		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
	}
	[ClientRpc]
	public void RegisterPlayerClientRpc(NetworkObjectReference playerRef)
	{
		if(playerRef.TryGet(out NetworkObject player))
		{
		    if (!alivePlayers.Contains(player))
			{
				alivePlayers.Add(player);
			}
		}
	}
	
	[ServerRpc]
	public void UnregisterPlayerServerRpc(NetworkObjectReference playerRef)
	{
		UnregisterPlayerClientRpc(playerRef);
	}
	
	[ClientRpc]
	public void UnregisterPlayerClientRpc(NetworkObjectReference playerRef)
	{
		if(playerRef.TryGet(out NetworkObject player))
		{
		    alivePlayers.Remove(player);
		}
	}

	public NetworkObject GetRandomPlayer()
	{
		if (alivePlayers.Count > 0)
		{
			int index = UnityEngine.Random.Range(0, alivePlayers.Count);
			NetworkObject selectedPlayer = alivePlayers[index];

			if (selectedPlayer == null)
			{
				Debug.LogError($"Selected player at index {index} is null!");
			}
			else
			{
				Debug.Log($"Selected random player at index {index}: {selectedPlayer.name} (IsSpawned: {selectedPlayer.IsSpawned})");
			}

			return selectedPlayer;
		}
		else
		{
			Debug.LogError("No alive players, the game should have ended?");
			return null;
		}
	}


	public void RespawnPlayers()
	{
		StartCoroutine(WaitForHouseMapToLoad("HouseMapLevel"));
	}
	private IEnumerator WaitForHouseMapToLoad(string sceneName)
	{
		// Check if the scene is already loaded
		Scene scene = SceneManager.GetSceneByName(sceneName);
		if (scene.isLoaded)
		{
			// Scene is already loaded, so we can proceed directly
			HandleSceneLoadedClientRpc(sceneName);
			yield break;  // Skip the coroutine if the scene is already loaded
		}

		// Wait until the additive scene is fully loaded
		while (!SceneManager.GetSceneByName(sceneName).isLoaded)
		{
			yield return null;
		}

		// Now wait an extra frame just in case objects are still initializing
		yield return null;

		// Find the HouseMapGenerator in the loaded scene
		HandleSceneLoadedClientRpc(sceneName);
	}

	[ClientRpc]
	private void HandleSceneLoadedClientRpc(string sceneName)
	{
		switch (sceneName)
		{
			case "HouseMapLevel":
				StartCoroutine(WaitUntilLevelGeneratedToSpawnPlayer());
				break;
			default:
				Debug.LogError("Could not find scene");
				break;
		}

	}
	private IEnumerator WaitUntilLevelGeneratedToSpawnPlayer()
	{
		HouseMapGenerator houseMapGenerator = null;

		// Wait until HouseMapGenerator is found
		while ((houseMapGenerator = FindFirstObjectByType<HouseMapGenerator>()) == null)
		{
			yield return null;
		}

		// Wait until the level is generated
		yield return new WaitUntil(() => houseMapGenerator.isLevelGenerated);
		loadingScreenManager.HideSleepingLoadingScreen();

		Vector3 baseSpawnPos = houseMapGenerator.GetPlayerSpawnPosition();

		if (!spawnedPlayers.Value && IsServer)
		{
			SpawnPlayers(baseSpawnPos);
			spawnedPlayers.Value = true;
		}
		else if (spawnedPlayers.Value)
		{
			foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
			{
				var playerObj = client.PlayerObject;
				if (playerObj != null)
				{
					if (IsServer)
					{
						Vector3 spawnPos = DetermineSpawnPosition(baseSpawnPos);
						playerObj.transform.position = spawnPos;
						MovePlayerToSpawnPositionClientRpc(playerObj, spawnPos);
					}
				}
			}
		}

	}

	[ClientRpc]
	private void MovePlayerToSpawnPositionClientRpc(NetworkObjectReference playerObjRef, Vector3 spawnPos)
	{
		if (playerObjRef.TryGet(out NetworkObject playerObj))
		{
			if (playerObj != null && playerObj.IsOwner)
			{
				// If this player object is owned by the client, move it to the spawn position
				playerObj.transform.position = spawnPos;
			}

		}
	}

	public void SpawnPlayers(Vector3 position)
	{
		if (IsServer)
		{
			foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
			{
				GameObject player = Instantiate(playerPrefab, DetermineSpawnPosition(position), Quaternion.identity);
				NetworkObject playerNetworkObject = player.GetComponent<NetworkObject>();
				playerNetworkObject.SpawnAsPlayerObject(clientId, true);
				RegisterPlayerClientRpc(playerNetworkObject);
				Debug.Log($"Spawned player {clientId} at {playerNetworkObject.transform.position}");
			}
		}
	}

	private Vector3 DetermineSpawnPosition(Vector3 pos)
	{
		Vector3[] offsets = new Vector3[]
		{
			new Vector3(2, 0, 0),
			new Vector3(-2, 0, 0),
			new Vector3(0, 0, 2),
			new Vector3(0, 0, -2)
		};
		Vector3 spawnPos = new Vector3(pos.x + offsets[spawnIndex.Value].x, pos.y + 2, pos.z + offsets[spawnIndex.Value].z);
		spawnIndex.Value = (spawnIndex.Value + 1) % MAX_PLAYERS;
		return spawnPos;

	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (IsServer)
		{
			GameManager.Instance.onGameStart -= RespawnPlayers;
		}
	}

}
