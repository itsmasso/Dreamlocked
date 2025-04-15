using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LightScript : NetworkBehaviour
{

	const float MIN_FLICKER_TIME = 0.5f;
	const float MAX_FLICKER_TIME = 1f;
	[SerializeField] private Light lightSource;
	[SerializeField] private List<Renderer> lightMaterialList = new List<Renderer>();
	[SerializeField] private LayerMask enemyLayer;
	[SerializeField] private LayerMask playerLayer;
	[SerializeField] private LayerMask obstacleLayer;
	[SerializeField] private LayerMask groundLayer;
	[SerializeField] private LayerMask interactableMoveables;
	private float lightRange;
	private bool isLightOn;
	private bool playerSeesLight;
	private int obstacleLayers;
	[SerializeField] private HashSet<NetworkObject> enemiesInLight = new HashSet<NetworkObject>();
	[SerializeField] private Collider[] enemyColliders;

	private float Timer;

	void Awake()
	{
		lightRange = lightSource.range;
	}
	void Start()
	{

		// The comment out line would make the flickering all different and random
		//Timer = Random.Range(MIN_FLICKER_TIME, MAX_FLICKER_TIME);
		Timer = MIN_FLICKER_TIME;
		TurnLightsOn();
		obstacleLayers = obstacleLayer.value | groundLayer.value | interactableMoveables;

	}

	void Update()
	{
		if (IsServer)
		{
			DetectEnemiesInLight();
			CheckIfEnemyExitLight();
			
		}
		CheckLightStatus();
		if (IsPlayerNear(lightSource) || CanPlayerSeeLight(lightSource))
		{
			playerSeesLight = true;

		}
		else
		{
			playerSeesLight = false;
			lightSource.enabled = false;
			TurnOffMaterialLight();
		}

	}

	private void TurnOnMaterialLight()
	{
		foreach (Renderer renderer in lightMaterialList)
		{
			Material targetMat = renderer.materials.FirstOrDefault(mat => mat.name.Contains("Lamp"));
			targetMat.EnableKeyword("_EMISSION");

			//material.SetColor("_EmissionColor", Color.white * 2f);
		}
	}

	private void TurnOffMaterialLight()
	{
		foreach (Renderer renderer in lightMaterialList)
		{
			Material targetMat = renderer.materials.FirstOrDefault(mat => mat.name.Contains("Lamp"));
			targetMat.DisableKeyword("_EMISSION");
		}
	}

	private bool IsPlayerNear(Light light)
	{
		Collider[] colliders = Physics.OverlapSphere(transform.position, light.range, playerLayer);
		foreach (Collider collider in colliders)
		{
			if (collider != null)
			{
				NetworkObject networkObject = collider.GetComponent<NetworkObject>();
				if (networkObject.IsOwner)
				{
					return true;
				}

			}
		}
		return false;
	}

	private bool CanPlayerSeeLight(Light light)
	{
		foreach (NetworkObject player in PlayerNetworkManager.Instance.alivePlayers)
		{
			if (!player.IsOwner || player == null)
				continue;

			PlayerCamera playerCamera = player.GetComponent<PlayerCamera>();
			if (playerCamera != null)
			{
				if (playerCamera.CheckForLightVisibility(light))
				{
					if (Vector3.Distance(transform.position, player.transform.position) >= Camera.main.farClipPlane / 2f)
					{
						light.shadows = LightShadows.None;
					}
					else
					{
						light.shadows = LightShadows.Soft;
					}
					return true;
				}


			}

		}
		return false;
	}


	private void CheckLightStatus()
	{
		if (GFClockManager.Instance != null)
		{
			switch (GFClockManager.Instance.GetMQThreatLevel())
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
		}else
		{
		    TurnLightsOn();
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
			if (playerSeesLight)
			{
				lightSource.enabled = !lightSource.enabled;
				if (lightSource.enabled)
					TurnOnMaterialLight();
				else
					TurnOffMaterialLight();
			}
			// The comment out line would make the flickering all different and random
			//Timer = Random.Range(MIN_FLICKER_TIME, MAX_FLICKER_TIME);
			Timer = MIN_FLICKER_TIME;
		}
	}

	private void TurnLightsOff()
	{
		isLightOn = false;
		lightSource.enabled = false;
		TurnOffMaterialLight();
	}

	private void TurnLightsOn()
	{
		isLightOn = true;
		if (playerSeesLight)
		{
			lightSource.enabled = true;
			TurnOnMaterialLight();
		}
	}

	//add clientrpc later maybe

	[ServerRpc]
	private void EnemyEnteredLightServerRpc(NetworkObjectReference enemyNetObjRef)
	{
		if (enemyNetObjRef.TryGet(out NetworkObject enemyNetObj))
		{
			if (enemyNetObj.TryGetComponent(out IAffectedByLight enemy))
			{
				enemy.EnteredLight();
			}
		}
	}

	[ServerRpc]
	private void EnemyExitLightServerRpc(NetworkObjectReference enemyNetObjRef)
	{
		if (enemyNetObjRef.TryGet(out NetworkObject enemyNetObj))
		{
			if (enemyNetObj.TryGetComponent(out IAffectedByLight enemy))
			{
				enemy.ExitLight();
			}
		}
	}

	private void DetectEnemiesInLight()
	{
		if (!isLightOn)
		{
			return;
		}
		enemyColliders = Physics.OverlapSphere(lightSource.transform.position, lightSource.range, enemyLayer);
		// possibly need to add pointLightSource as a second collider
		foreach (Collider enemyCollider in enemyColliders)
		{
			NetworkObject enemyNetObj = enemyCollider.GetComponent<NetworkObject>();
			if (enemyNetObj != null && enemyNetObj.IsSpawned)
			{
				IAffectedByLight monsterAffectedByLight = enemyCollider.GetComponent<IAffectedByLight>();
				if (monsterAffectedByLight != null)
				{
					if (!IsDirectlyLit(enemyCollider.transform)) continue;

					// This check could possibly lead to bugs
					// I think it check enemyNetObj not NetworkObject
					if (enemiesInLight.Contains(enemyNetObj)) continue;

					enemiesInLight.Add(enemyNetObj);
					monsterAffectedByLight.EnteredLight();
					EnemyEnteredLightServerRpc(enemyNetObj);
				}
			}


		}
	}

	private void CheckIfEnemyExitLight()
	{
		foreach (NetworkObject enemy in enemiesInLight.ToList())
		{
			// Ensure the enemy is not null before processing
			if (enemy == null)
			{
				enemiesInLight.Remove(enemy);
				continue;
			}
			IAffectedByLight affectedByLight = enemy.GetComponent<IAffectedByLight>();
			if (affectedByLight != null)
			{
				if (enemy != null && !IsDirectlyLit(enemy.transform) || Vector3.Distance(enemy.transform.position, lightSource.transform.position) >= lightSource.range)
				{
					affectedByLight.ExitLight();
					enemiesInLight.Remove(enemy);
					EnemyExitLightServerRpc(enemy);
				}
			}
		}
	}

	private bool IsDirectlyLit(Transform enemy)
	{
		if (!isLightOn)
		{
			return false;
		}
		Vector3 dirToEnemy = (enemy.position - lightSource.transform.position).normalized;
		float distance = Vector3.Distance(lightSource.transform.position, enemy.position);
		//Debug.DrawRay(lightSource.transform.position, dirToEnemy*distance, Color.yellow);
		if (Physics.Raycast(lightSource.transform.position, dirToEnemy, out RaycastHit hit, distance, obstacleLayers))
		{
			if (hit.collider != null)
			{
				return false;
			}

		}

		return true;

	}

}
