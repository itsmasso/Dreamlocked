using UnityEngine;
using System.Collections.Generic;

public class GameManager : Singleton<GameManager>
{
	[SerializeField] private ProceduralRoomGeneration levelGenerator;
	[SerializeField] private List<Transform> spawnPoints;
	void Start()
	{
		levelGenerator.Generate();
		//UnitManager.Instance.SpawnPlayer(levelGenerator.GetPlayerSpawnPosition());
		Vector3 playerRoomSpawnPosition = levelGenerator.GetPlayerSpawnPosition(); //maybe later make it random rooms?
		for(int i = 0; i < spawnPoints.Count; i++)
		{
			spawnPoints[i].position = new Vector3(playerRoomSpawnPosition.x + i, playerRoomSpawnPosition.y, playerRoomSpawnPosition.z + i);
		}
		
	}

	// Update is called once per frame
	void Update()
	{
		
	}
}
