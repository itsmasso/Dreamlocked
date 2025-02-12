using UnityEngine;
using Unity.Netcode;
public class LevelManager : NetworkSingleton<LevelManager>
{
	public ProceduralRoomGeneration levelGenerator;
	private NetworkVariable<int> seed = new NetworkVariable<int>();
	public override void OnNetworkSpawn()
	{	
		if(IsServer)
		{
			seed.Value = Random.Range(1, 999999);
		}
		levelGenerator.Generate(seed.Value);
		
	}
	//for testing purposes
	[ServerRpc(RequireOwnership = false)]
	public void SetPlayerSpawnPosServerRpc(ulong networkObjectId)
	{
		if(IsServer)
		{
			NetworkObject playerObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId];
			
			SetPlayerSpawnPosClientRpc(playerObject);
			
		}
	}
	//for testing purposes
	[ClientRpc]
	public void SetPlayerSpawnPosClientRpc(NetworkObjectReference ownerPlayerObjectRef)
	{
		ownerPlayerObjectRef.TryGet(out NetworkObject playerNetworkObject);
		playerNetworkObject.transform.position = levelGenerator.GetPlayerSpawnPosition();
		
		Debug.Log($"Player on client {OwnerClientId} spawned");
	}
}
