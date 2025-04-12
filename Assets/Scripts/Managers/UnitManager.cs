using System;
using System.Collections.Generic;
using System.Linq;
using Pathfinding;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UnitManager : MonoBehaviour
{
	
	[SerializeField] private GameObject lurkerPrefab;
	private List<NetworkObject> spawnedEnemies = new List<NetworkObject>();
	[SerializeField] private GameObject MQMonsterPrefab;
	[SerializeField] private HouseMapGenerator houseMapGenerator;
	[SerializeField] private float minSpawnDistFromPlayers;
	[SerializeField] private float maxSpawnDistFromPlayers;
	private float timer;
	[SerializeField] private float whenToSpawnEnemies = 5f;
	private bool alreadySpawnedEnemies= false;
	private bool canSpawnEnemies = false;
	[SerializeField] private string defaultScene;

	void Start()
	{
		//GameManager.Instance.onGamePlaying += CanSpawnEnemies;
		
	}
	
	void OnEnable()
    {
        if (GameManager.Instance != null)
		{
			GameManager.Instance.onGameStart += CanSpawnEnemies;
			GameManager.Instance.onNextLevel += DespawnAllEnemies;
		}
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
		{
			GameManager.Instance.onGameStart -= CanSpawnEnemies;
			GameManager.Instance.onNextLevel -= DespawnAllEnemies;
		}
    }
		
	private Vector3 GetPositionInRangeOfPlayers()
	{
		Vector3 bestPositionToSpawn = transform.position;
		float maxMinDistance = 0f;
		
		foreach(Vector3 hallwayPos in houseMapGenerator.GetHallwayList().ToList()){
			float minDistToPlayers = float.MaxValue;
			
			// Calculate the minimum distance from this hallway position to all players
			foreach(NetworkObject player in PlayerNetworkManager.Instance.alivePlayers)
			{
				float dist = Vector3.Distance(hallwayPos, player.transform.position);
				if(dist < minDistToPlayers)
				{
					minDistToPlayers = dist;
				}
			}
			// If this position meets the minimum distance requirement, return it immediately
			if(minDistToPlayers >= minSpawnDistFromPlayers && minDistToPlayers <= maxSpawnDistFromPlayers)
			{
				return hallwayPos;
			}	
			// Otherwise, track the position that is farthest from the nearest player
			if(minDistToPlayers > maxMinDistance)
			{
				maxMinDistance = minDistToPlayers;
				bestPositionToSpawn = hallwayPos;
			}
		}
		Debug.Log("No valid positions");
		// If no valid position was found, return the farthest possible one
		return bestPositionToSpawn;
	}
	
	private void CanSpawnEnemies()
	{
	    canSpawnEnemies = true;
	}
    void Update()
    {
        if(!alreadySpawnedEnemies && canSpawnEnemies)
        {
            timer += Time.deltaTime;
			if(timer >= whenToSpawnEnemies)
			{
				SpawnStartEnemies();
				timer = 0f;
				alreadySpawnedEnemies = true;
			}
        }
    }

    private void SpawnStartEnemies()
	{
		SpawnLurker();
		SpawnMannequinMonsters();
	}
	
	private void SpawnLurker()
	{
		Vector3 spawnPosition = GetPositionInRangeOfPlayers();
	    SpawnMonster(lurkerPrefab, new Vector3(spawnPosition.x, spawnPosition.y + lurkerPrefab.GetComponent<FollowerEntity>().height/2, spawnPosition.z), Quaternion.identity);
	}

	public void SpawnMonster(GameObject monsterPrefab, Vector3 position, Quaternion rotation)
	{	
		GameObject monsterObject = Instantiate(monsterPrefab, position, rotation);
		spawnedEnemies.Add(monsterObject.GetComponent<NetworkObject>());
		monsterObject.GetComponent<NetworkObject>().Spawn(true);
		
	}
	/*****************************************************************
	* SpawnMannequinMonsters
	*****************************************************************
	* Author: Dylan Werelius
	*****************************************************************
	* Description:
		This function will spawn a mannequin monster in each of the
		rooms. It uses functions from the HouseMapGenerator to get
		the positions of all the rooms and then it creates instances
		of the MQMonster objects in each room.
	*****************************************************************/
	
	private void SpawnMannequinMonsters()
	{
		foreach(Vector3 roomPos in houseMapGenerator.GetNormalRoomsList().ToList())
		{
			// Spawn the monster on the first floor
			if (roomPos.y == -8.00)
			{
				SpawnMonster(MQMonsterPrefab, new Vector3(roomPos.x, roomPos.y + MQMonsterPrefab.GetComponentInChildren<CapsuleCollider>().height/16, roomPos.z), Quaternion.identity);
				//Debug.Log("Mannequin Spawned at: " + roomPos);
			}
		}
	}
	
	public void DespawnAllEnemies()
	{
		if(spawnedEnemies.Count > 0)
		{
		    foreach (var enemy in spawnedEnemies)
			{
				if (enemy != null && enemy.IsSpawned)
				{
					enemy.Despawn(true);
					//Destroy(enemy.gameObject);
				}
			}
		}
		spawnedEnemies.Clear();
		alreadySpawnedEnemies = false;
		canSpawnEnemies = false;
		Debug.Log("All enemies despawned.");
	}

    void OnDestroy()
    {
        DespawnAllEnemies();
    }

}
