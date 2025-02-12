using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class GameManager : NetworkSingleton<GameManager>
{
	//handle scenes
	protected override void Awake() {
		base.Awake();
		DontDestroyOnLoad(gameObject);
	}
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{
		
	}

}
