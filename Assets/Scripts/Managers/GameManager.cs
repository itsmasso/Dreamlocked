using UnityEngine;
using System.Collections.Generic;


public class GameManager : Singleton<GameManager>
{
	[SerializeField] private ProceduralRoomGeneration levelGenerator;

	[SerializeField] private GameObject player;
	
	
	void Start()
	{
		int seed = Random.Range(1, 999999);
		levelGenerator.Generate(seed);
		
		
		
	}

	// Update is called once per frame
	void Update()
	{
		
	}
	
}
