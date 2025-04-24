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
	private int currentSpectateIndex = 0;
	[SerializeField] private GameObject playerPrefab;
	public List<NetworkObject> alivePlayers = new List<NetworkObject>();
	public NetworkVariable<int> alivePlayersCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	public NetworkVariable<int> spawnIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone);
	public static event Action onRespawnPlayer;
	[SerializeField] private ScreenManager screenManager;
	private NetworkVariable<Vector3> currentSpawnPos = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	private Dictionary<ulong, List<ItemData>> savedInventories = new();
	void Start()
	{
		if (IsServer)
		{
			GameManager.Instance.onGameStart += RespawnPlayers;
			GameManager.Instance.onNextLevel += DespawnPlayers;
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
	}

	[Rpc(SendTo.Everyone)]
	public void RegisterPlayerClientRpc(NetworkObjectReference playerRef)
	{
		if (playerRef.TryGet(out NetworkObject player))
		{
			if (!alivePlayers.Contains(player))
			{
				alivePlayers.Add(player);
				if (IsServer) alivePlayersCount.Value++;
			}
		}
	}

	[Rpc(SendTo.Server)]
	public void UnregisterPlayerServerRpc(NetworkObjectReference playerRef)
	{
		UnregisterPlayerClientRpc(playerRef);
		alivePlayersCount.Value--;
	}

	[Rpc(SendTo.Everyone)]
	public void UnregisterPlayerClientRpc(NetworkObjectReference playerRef)
	{
		if (playerRef.TryGet(out NetworkObject player))
		{
			if (player.IsOwner)
			{
				alivePlayers.Remove(player);
				Debug.Log(alivePlayersCount.Value);
			}
		}
		if (IsServer && alivePlayersCount.Value <= 0)
		{
			alivePlayersCount.Value = 0;
			GameManager.Instance.ChangeGameState(GameState.GameOver);
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
	public void SpectatePlayers()
	{
		if (alivePlayers.Count > 0)
		{

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
		//when adding more maps, make sure to generalize map generators like attatch interface with get generator function or abstract class
		HouseMapGenerator houseMapGenerator = null;

		// Wait until HouseMapGenerator is found
		while ((houseMapGenerator = FindFirstObjectByType<HouseMapGenerator>()) == null)
		{
			yield return null;
		}

		// Wait until the level is generated
		yield return new WaitUntil(() => houseMapGenerator.isLevelGenerated);
		screenManager.HideSleepingLoadingScreen();

		if (IsServer) currentSpawnPos.Value = houseMapGenerator.GetPlayerSpawnPosition();


		if (IsServer)
		{
			SpawnPlayers(currentSpawnPos.Value);
		}

	}
	public NetworkObject GetNextPlayerToSpectate(bool reset = false)
	{
		if (alivePlayers.Count == 0)
		{
			Debug.LogWarning("No alive players to spectate.");
			return null;
		}

		if (reset)
			currentSpectateIndex = 0;
		else
			currentSpectateIndex = (currentSpectateIndex + 1) % alivePlayers.Count;

		NetworkObject selectedPlayer = alivePlayers[currentSpectateIndex];

		if (selectedPlayer == null)
		{
			Debug.LogError($"Spectate player at index {currentSpectateIndex} is null!");
			return null;
		}

		Debug.Log($"Spectating player at index {currentSpectateIndex}: {selectedPlayer.name} (IsSpawned: {selectedPlayer.IsSpawned})");

		return selectedPlayer;
	}
	// //use this function if we need logic to move players if they are out of bounds for example or something
	// [ClientRpc]
	// private void MovePlayerToSpawnPositionClientRpc(NetworkObjectReference playerObjRef, Vector3 spawnPos)
	// {
	// 	if (playerObjRef.TryGet(out NetworkObject playerObj))
	// 	{
	// 		if (playerObj != null && playerObj.IsOwner)
	// 		{
	// 			// If this player object is owned by the client, move it to the spawn position
	// 			playerObj.transform.position = spawnPos;
	// 		}

	// 	}
	// }

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
				player.GetComponent<PlayerHealth>().ResetHealth();
				var inventory = LoadInventory(playerNetworkObject.OwnerClientId);
				player.GetComponent<PlayerInventory>().EnsureEmptyInventorySlots(4);
				foreach (var itemData in inventory)
				{
					player.GetComponent<PlayerInventory>().RepopulateItem(itemData);
				}
				player.GetComponent<PlayerInventory>().RequestServerToSelectNewItemRpc(0);
				Debug.Log($"Spawned player {clientId} at {playerNetworkObject.transform.position}");
			}
		}
	}

	public void DespawnPlayers()
	{
		if (IsServer)
		{
			foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
			{
				if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
				{
					NetworkObject playerObject = client.PlayerObject;

					if (playerObject != null)
					{
						SaveInventory(playerObject.OwnerClientId, playerObject.GetComponent<PlayerInventory>().SyncedInventory);
						playerObject.Despawn(true);
						ClearAlivePlayersClientRpc();
						Debug.Log($"Despawned {clientId}: {playerObject.name}");
					}
				}
			}
		}
	}
	public void SaveInventory(ulong clientId, NetworkList<ItemData> inventoryList)
	{
		List<ItemData> copy = new List<ItemData>();
		foreach (var item in inventoryList)
		{
			copy.Add(item);
		}

		savedInventories[clientId] = copy;
		Debug.Log($"[SaveInventory] Saved {copy.Count} items for clientId: {clientId}");
		foreach (var item in copy)
		{
			Debug.Log($"[SaveInventory] Item saved - ID: {item.id}, UID: {item.uniqueId}, Charge: {item.itemCharge}, Uses: {item.usesRemaining}");
		}
	}

	public List<ItemData> LoadInventory(ulong clientId)
	{
		if (!savedInventories.ContainsKey(clientId))
		{
			Debug.Log($"[LoadInventory] No saved inventory found for clientId: {clientId}, returning empty inventory.");
			ItemData emptyItem = new ItemData { id = -1, itemCharge = 0, usesRemaining = 0, uniqueId = -1 };
			List<ItemData> newEmptyInventory = new List<ItemData>();
			while (newEmptyInventory.Count < 4)
				newEmptyInventory.Add(emptyItem);
			return newEmptyInventory;
		}

		List<ItemData> copy = new List<ItemData>();
		foreach (var item in savedInventories[clientId])
		{
			copy.Add(item);
		}

		Debug.Log($"[LoadInventory] Loaded {copy.Count} items for clientId: {clientId}");
		foreach (var item in copy)
		{
			Debug.Log($"[LoadInventory] Item loaded - ID: {item.id}, UID: {item.uniqueId}, Charge: {item.itemCharge}, Uses: {item.usesRemaining}");
		}

		return copy;
	}
	public void ClearInventory(ulong clientId)
	{
		savedInventories.Remove(clientId); // Safe even if key doesn't exist
	}

	[ClientRpc]
	private void ClearAlivePlayersClientRpc()
	{
		alivePlayers.Clear();
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
			GameManager.Instance.onNextLevel -= DespawnPlayers;
		}
	}

}
