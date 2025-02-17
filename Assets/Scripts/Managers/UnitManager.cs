using System;
using System.Collections.Generic;
using Pathfinding;
using Unity.Netcode;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
	[SerializeField] private GameObject monster;
	
	public void SpawnMonster(Vector3 position)
	{
		GameObject monsterObject = Instantiate(monster, new Vector3(position.x, position.y + monster.GetComponent<FollowerEntity>().height/2, position.z), Quaternion.identity);
		monsterObject.GetComponent<NetworkObject>().Spawn(true);
	}
}
