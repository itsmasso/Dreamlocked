using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class LightScript : NetworkBehaviour
{

	const float MIN_FLICKER_TIME = 0.5f;
	const float MAX_FLICKER_TIME = 1f;
	private GFClockManager manager;
	[SerializeField] private Light lightSource;
	[SerializeField] private Light secondaryLightSource;
	[SerializeField] private LayerMask enemyLayer;
	[SerializeField] private LayerMask obstacleLayer;
	[SerializeField] private LayerMask groundLayer;
	private int obstacleLayers;
	[SerializeField] private HashSet<NetworkObject> enemiesInLight = new HashSet<NetworkObject>();
	[SerializeField] private Collider[] enemyColliders;
	private float Timer;
	void Start()
	{
		manager = GFClockManager.Instance;
		// The comment out line would make the flickering all different and random
		//Timer = Random.Range(MIN_FLICKER_TIME, MAX_FLICKER_TIME);
		Timer = MIN_FLICKER_TIME;
		obstacleLayers = obstacleLayer.value | groundLayer.value;
	}

	
	void Update()
	{
		if(!IsServer) return;
		DetectEnemiesInLight();
		CheckIfEnemyExitLight();
		CheckLightStatus();
	}
	
	private void CheckLightStatus()
	{
		switch(manager.GetMQThreatLevel())
		{
			case MQThreatLevel.PASSIVE:
				TurnLightsOn();
				break;
			case MQThreatLevel.ACTIVATING:
				FlickerLights();
				break;
			case MQThreatLevel.AWAKENED:
				TurnLightsOff();
				break;
			default:
				TurnLightsOff();
				Debug.Log("ERROR: LightScript.cs - MQThreatLevel is Broken");
				break;
		}
	}
	
	private void FlickerLights()
	{
		if (Timer > 0)
		{
			Timer -= Time.deltaTime;
		}

		if (Timer <= 0)
		{
			lightSource.enabled = !lightSource.enabled;
			if (secondaryLightSource)
			{
				secondaryLightSource.enabled = !secondaryLightSource.enabled;
			}
			// The comment out line would make the flickering all different and random
			//Timer = Random.Range(MIN_FLICKER_TIME, MAX_FLICKER_TIME);
			Timer = MIN_FLICKER_TIME;
		}
	}
	
	private void TurnLightsOff()
	{
		lightSource.enabled = false;
		if (secondaryLightSource)
		{
			secondaryLightSource.enabled = false;
		}
	}

	private void TurnLightsOn()
	{
		lightSource.enabled = true;
		if (secondaryLightSource)
		{
			secondaryLightSource.enabled = true;
		}
	}
	
	//add clientrpc later maybe
	
	[ServerRpc]
	private void EnemyEnteredLightServerRpc(NetworkObjectReference enemyNetObjRef)
	{
	    if(enemyNetObjRef.TryGet(out NetworkObject enemyNetObj))
	    {
	        if(enemyNetObj.TryGetComponent(out IAffectedByLight enemy))
	        {
	            enemy.EnteredLight();
	        }
	    }
	}
	
	[ServerRpc]
	private void EnemyExitLightServerRpc(NetworkObjectReference enemyNetObjRef)
	{
	    if(enemyNetObjRef.TryGet(out NetworkObject enemyNetObj))
	    {
	        if(enemyNetObj.TryGetComponent(out IAffectedByLight enemy))
	        {
	            enemy.ExitLight();
	        }
	    }
	}
	
	private void DetectEnemiesInLight()
	{
		if (!lightSource.enabled)
		{
			return;
		}
		enemyColliders = Physics.OverlapSphere(lightSource.transform.position, lightSource.range, enemyLayer);
		// possibly need to add pointLightSource as a second collider
		foreach(Collider enemyCollider in enemyColliders)
		{
			NetworkObject enemyNetObj = enemyCollider.GetComponent<NetworkObject>();
			if(enemyNetObj != null && enemyNetObj.IsSpawned)
			{
			    IAffectedByLight monsterAffectedByLight = enemyCollider.GetComponent<IAffectedByLight>();
				if(monsterAffectedByLight != null)
				{
					if(!IsDirectlyLit(enemyCollider.transform)) continue;

					// This check could possibly lead to bugs
					// I think it check enemyNetObj not NetworkObject
					if(enemiesInLight.Contains(enemyNetObj)) continue;
					
					enemiesInLight.Add(enemyNetObj);
					monsterAffectedByLight.EnteredLight();
					EnemyEnteredLightServerRpc(enemyNetObj);
				}
			}
			
			
		}
	}
	
	private void CheckIfEnemyExitLight()
	{
		foreach(NetworkObject enemy in enemiesInLight.ToList())
		{
			if(enemy != null && !IsDirectlyLit(enemy.transform) || Vector3.Distance(enemy.transform.position, lightSource.transform.position) >= lightSource.range)
			{
				enemy.GetComponent<IAffectedByLight>().ExitLight();
				enemiesInLight.Remove(enemy);
				EnemyExitLightServerRpc(enemy);
			}
		}
	}
	
	private bool IsDirectlyLit(Transform enemy)
	{
		if (!lightSource.enabled)
		{
			return false;
		}
		Vector3 dirToEnemy = (enemy.position - lightSource.transform.position).normalized;
		float distance = Vector3.Distance(lightSource.transform.position, enemy.position);
		//Debug.DrawRay(lightSource.transform.position, dirToEnemy*distance, Color.yellow);
		if(Physics.Raycast(lightSource.transform.position, dirToEnemy, out RaycastHit hit, distance, obstacleLayers))
		{
			if(hit.collider != null)
			{
				return false;
			}
			
		}

		return true;
		
	}
}
