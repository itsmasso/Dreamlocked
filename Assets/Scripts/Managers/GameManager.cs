using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class GameManager : NetworkSingleton<GameManager>
{
	public ProceduralRoomGeneration levelGenerator;

	public NetworkObject playerOwner;

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
	public void SetPlayerOwnerServerRpc(ulong networkObjectId)
	{
		if(IsServer)
		{
			NetworkObject playerObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId];
			playerOwner = playerObject;
			
			SetOwnerPlayerClientRpc(playerOwner);
			
		}
	}
	//for testing purposes
	[ClientRpc]
	public void SetOwnerPlayerClientRpc(NetworkObjectReference ownerPlayerObjectRef)
	{
		ownerPlayerObjectRef.TryGet(out NetworkObject playerNetworkObject);
		playerOwner = playerNetworkObject;
		playerNetworkObject.transform.position = levelGenerator.GetPlayerSpawnPosition();
		
		Debug.Log($"Player on client {OwnerClientId} spawned");
	}
	

	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{
		
	}

}
