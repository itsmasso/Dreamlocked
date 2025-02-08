using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/*
credit: helped from Tarodev architecture video
template for creation of singletons

Notes on Abstract classes:
- declaring a function virtual means other classes inheriting that function can override them
- base.function() to call the original function with the added code in the override function.
 */

//regular singleton
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
	public static T Instance { get; private set; }

	protected virtual void Awake()
	{
		//if an instance of this singleton already exists, destroy this one. (only one instance of this singleton can exist at a time)
		if (Instance != null)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this as T;
	}

}

public abstract class NetworkSingleton<T> : NetworkBehaviour where T : NetworkBehaviour
{
	public static T Instance { get; private set; }

	protected virtual void Awake()
	{
		//if an instance of this singleton already exists, destroy this one. (only one instance of this singleton can exist at a time)
		if (Instance != null)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this as T;
	}

}



//singleton that won't be destroyed through scene loads. (useful for music that plays during load screens)
public abstract class PersistentSingleton<T> : Singleton<T> where T : MonoBehaviour
{
	protected override void Awake()
	{
		base.Awake();
		DontDestroyOnLoad(gameObject);
	}
}