using UnityEngine;
using Unity.Netcode;
using Pathfinding;

public class MonsterBaseScript : NetworkBehaviour
{
	private enum MonsterStates
	{
		Roaming,
		Stalking,
		Chasing
		

	}
	[SerializeField] private FollowerEntity ai;
	[SerializeField] private GameObject seeker;
	
	private void Start() {
		
	}
	
	private void Update() {
		
	}
}
