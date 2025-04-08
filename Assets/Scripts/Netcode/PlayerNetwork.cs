using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
public class PlayerNetwork : NetworkBehaviour
{
    public const int MAX_PLAYERS = 4;
    [SerializeField] private GameObject playerPrefab;
    public static List<Transform> playerTransforms = new List<Transform>();
	public static List<NetworkObject> alivePlayers = new List<NetworkObject>();
	public NetworkVariable<int> spawnIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	
    public void SpawnPlayers(Vector3 position)
	{
		if(IsServer)
		{
			foreach(ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
			{
				GameObject player = Instantiate(playerPrefab, DetermineSpawnPosition(position), Quaternion.identity);
				NetworkObject playerNetworkObject = player.GetComponent<NetworkObject>();
				playerNetworkObject.SpawnAsPlayerObject(clientId, true);	
				playerTransforms.Add(playerNetworkObject.transform);

				alivePlayers.Add(playerNetworkObject);
				spawnIndex.Value = (spawnIndex.Value + 1) % MAX_PLAYERS;
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
		return new Vector3(pos.x + offsets[spawnIndex.Value].x, pos.y + 2, pos.z + offsets[spawnIndex.Value].z);
		
	}

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        alivePlayers.Clear();
        playerTransforms.Clear();
    }

}
