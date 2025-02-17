using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Steamworks;
using Unity.Netcode;
using Netcode.Transports.Facepunch;
using UnityEngine.SceneManagement;



public class PlayerSpawner : NetworkSingleton<PlayerSpawner>
{
	[SerializeField]
	private GameObject Player;
	public List<Transform> playerTransforms = new List<Transform>();
	void Start()
	{
		DontDestroyOnLoad(this.gameObject);
	}

	public override void OnNetworkSpawn()
	{
		NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneLoaded;
		NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
	}

	private void SceneLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
	{
		if(IsHost && sceneName == "GameScene")
		{
			foreach(ulong id in clientsCompleted)
			{
				GameObject player = Instantiate(Player, LevelManager.Instance.levelGenerator.GetPlayerSpawnPosition(), Quaternion.identity);
				NetworkObject playerNetworkObject = player.GetComponent<NetworkObject>();
				playerNetworkObject.SpawnWithOwnership(id, true);
				SetPlayerSpawnPositionServerRpc(playerNetworkObject);
				
			}
		}
	}
	
	//test
	private void OnClientConnected(ulong clientId)
	{
		if (!NetworkManager.Singleton.IsServer) return;

		GameObject player = Instantiate(Player, LevelManager.Instance.levelGenerator.GetPlayerSpawnPosition(), Quaternion.identity);
		NetworkObject playerNetworkObject = player.GetComponent<NetworkObject>();

		if (playerNetworkObject == null)
		{
			Debug.LogError("Player prefab is missing a NetworkObject!");
			return;
		}

		playerNetworkObject.SpawnWithOwnership(clientId, true);
		NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client);
		client.PlayerObject = playerNetworkObject;
		playerTransforms.Add(client.PlayerObject.transform);
		Debug.Log($"Spawned player {clientId} at {player.transform.position}");

	}
	

	
	[ServerRpc(RequireOwnership = false)]
	public void SetPlayerSpawnPositionServerRpc(NetworkObjectReference ownerPlayerObjectRef)
	{
		if(IsServer)
		{		
			SetPlayerSpawnPositionClientRpc(ownerPlayerObjectRef);
			
		}
	}
	
	[ClientRpc]
	public void SetPlayerSpawnPositionClientRpc(NetworkObjectReference ownerPlayerObjectRef)
	{
		ownerPlayerObjectRef.TryGet(out NetworkObject playerNetworkObject);
		playerNetworkObject.transform.position = LevelManager.Instance.levelGenerator.GetPlayerSpawnPosition();
		
	}

}