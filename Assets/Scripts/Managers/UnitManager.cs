using System;
using System.Collections.Generic;
using System.Linq;
using Pathfinding;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UnitManager : NetworkBehaviour
{

	[SerializeField] private GameObject lurkerPrefab;
	private List<NetworkObject> spawnedEnemies = new List<NetworkObject>();
	[SerializeField] private GameObject MQMonsterPrefab;
	[SerializeField] private HouseMapGenerator houseMapGenerator;
	[SerializeField] private float minSpawnDistFromPlayers;
	[SerializeField] private float maxSpawnDistFromPlayers;
	private bool canSpawnEnemies = false;
	[SerializeField] private string defaultScene;
	private HouseMapDifficultySettingsSO currentDifficultySetting;
	private float lurkerSpawnTimer;
	private int lurkerSpawnCount;
	private bool canSpawnLurker;
	private int mannequinSpawnCount;
	void Start()
	{
		if (IsServer)
		{
			currentDifficultySetting = GameManager.Instance.GetLevelLoader().currentHouseMapDifficultySetting;
			lurkerSpawnTimer = currentDifficultySetting.lurkerSpawnDelay;
			float rand = UnityEngine.Random.value;
			if (rand <= currentDifficultySetting.chanceToSpawnLurker)
				canSpawnLurker = true;
		}
	}

	void OnEnable()
	{
		if (GameManager.Instance != null)
		{
			GameManager.Instance.onGameStart += CanSpawnEnemies;
			GameManager.Instance.onNextLevel += DespawnAllEnemies;
			GameManager.Instance.onLobby += DespawnAllEnemies;
		}
	}

	void OnDisable()
	{
		if (GameManager.Instance != null)
		{
			GameManager.Instance.onGameStart -= CanSpawnEnemies;
			GameManager.Instance.onNextLevel -= DespawnAllEnemies;
			GameManager.Instance.onLobby -= DespawnAllEnemies;
		}
	}

	private Vector3 GetPositionInRangeOfPlayers()
	{
		Vector3 bestPositionToSpawn = transform.position;
		float maxMinDistance = 0f;

		foreach (Vector3 hallwayPos in houseMapGenerator.GetHallwayList().ToList())
		{
			float minDistToPlayers = float.MaxValue;

			// Calculate the minimum distance from this hallway position to all players
			foreach (NetworkObject player in PlayerNetworkManager.Instance.alivePlayers)
			{
				float dist = Vector3.Distance(hallwayPos, player.transform.position);
				if (dist < minDistToPlayers)
				{
					minDistToPlayers = dist;
				}
			}
			// If this position meets the minimum distance requirement, return it immediately
			if (minDistToPlayers >= minSpawnDistFromPlayers && minDistToPlayers <= maxSpawnDistFromPlayers)
			{
				return hallwayPos;
			}
			// Otherwise, track the position that is farthest from the nearest player
			if (minDistToPlayers > maxMinDistance)
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
		if (IsServer)
		{
			canSpawnEnemies = true;
			SpawnMannequinMonsters();
		}
	}
	void Update()
	{
		if (IsServer && canSpawnEnemies && lurkerSpawnCount < currentDifficultySetting.lurkerSpawnAmount && canSpawnLurker)
		{
			HandleLurkerSpawnTimer();
		}
		
	}

	private void HandleLurkerSpawnTimer()
	{
		if (lurkerSpawnTimer > 0)
		{
			lurkerSpawnTimer -= Time.deltaTime;
		}
		else if (lurkerSpawnTimer <= 0)
		{
			SpawnLurker();
			lurkerSpawnTimer = currentDifficultySetting.lurkerSpawnDelay;
		}
	}

	private void SpawnLurker()
	{
		Vector3 spawnPosition = GetPositionInRangeOfPlayers();
		GameObject lurker = Instantiate(lurkerPrefab, new Vector3(spawnPosition.x, spawnPosition.y + lurkerPrefab.GetComponent<FollowerEntity>().height / 2, spawnPosition.z), Quaternion.identity);
		lurker.GetComponent<LurkerMonsterScript>().houseMapGenerator = houseMapGenerator;

		spawnedEnemies.Add(lurker.GetComponent<NetworkObject>());
		lurker.GetComponent<NetworkObject>().Spawn(true);
		lurkerSpawnCount++;
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
		foreach (Room room in houseMapGenerator.GetNormalRoomComponents().ToList())
		{
			foreach (Transform monsterTransform in room.monsterTransforms)
			{
				if (mannequinSpawnCount <= currentDifficultySetting.mannequinSpawnAmount)
				{
					float rand = UnityEngine.Random.value;
					if (rand <= currentDifficultySetting.chanceToSpawnMannequin)
					{
						SpawnMonster(MQMonsterPrefab, new Vector3(monsterTransform.position.x, monsterTransform.position.y + MQMonsterPrefab.GetComponentInChildren<CapsuleCollider>().height/2, monsterTransform.position.z), Quaternion.identity);
						mannequinSpawnCount++;
					}
					
				}
			}
		}
	}

	public void DespawnAllEnemies()
	{
		if (IsServer)
		{
			if (spawnedEnemies.Count > 0)
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
			canSpawnEnemies = false;
			Debug.Log("All enemies despawned.");
		}
	}

	public override void OnDestroy()
	{
		DespawnAllEnemies();
	}

}
