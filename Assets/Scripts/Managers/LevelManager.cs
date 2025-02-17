using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
public class LevelManager : NetworkSingleton<LevelManager>
{
	public HouseMapGenerator levelGenerator;
	

	[SerializeField] private UnitManager unitManager;
	private NetworkVariable<int> seed = new NetworkVariable<int>();

    void Start()
    {
 
    }
    public override void OnNetworkSpawn()
	{	
		if(IsServer)
		{
			seed.Value = Random.Range(1, 999999);
		}
		levelGenerator.Generate(seed.Value);
		Invoke("SpawnMonster", 2f);
		
	}
	

	
	
	private void SpawnMonster()
	{
		unitManager.SpawnMonster(levelGenerator.GetRandomHallwayPosition());
	}
	
	
}
