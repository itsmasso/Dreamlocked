using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnvironmentLightScript : NetworkBehaviour
{

	const float MIN_FLICKER_TIME = 0.2f;
	const float MAX_FLICKER_TIME = 0.5f;
	[SerializeField] private Light lightSource;
	[SerializeField] private List<Renderer> lightMaterialList = new List<Renderer>();
	[SerializeField] private LayerMask playerLayer;
	private bool isLightOn;
	private bool playerSeesLight;

	[SerializeField] private DetectEnemyInLights lightScript;
	private float Timer;
	public LightFlicker lightFlicker;
	[SerializeField] private Sound3DSO lightBuzzSFX;
	private bool playingLightBuzz;
	public NetworkVariable<bool> isLightEnabled = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
	}
	void Start()
	{
		// The comment out line would make the flickering all different and random
		//Timer = Random.Range(MIN_FLICKER_TIME, MAX_FLICKER_TIME);

		playingLightBuzz = false;
		Timer = MIN_FLICKER_TIME;
		if(IsServer) TurnLightsOn();
	}

	void Update()
	{
		if (IsServer)
		{
			CheckLightStatus(); // Server controls threat level / flicker style
			FlickerLights();// Server toggles isLightEnabled.Value
		}

		// Locally check if this player should see the light
		bool shouldSeeLight = IsPlayerNear(lightSource) || CanPlayerSeeLight(lightSource);

		if (shouldSeeLight)
		{
			// If player should see the light, set based on networked flicker
			lightSource.enabled = isLightEnabled.Value;

			if (isLightEnabled.Value)
				TurnOnMaterialLight();
			else
				TurnOffMaterialLight();
		}
		else
		{
			// If player should NOT see light, force it off locally
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
		}
		else
		{
			TurnLightsOn();
		}
	}

	private void FlickerLights()
	{
		if (!IsServer) return;
		if (GFClockManager.Instance != null && GFClockManager.Instance.GetMQThreatLevel() == MQThreatLevel.ACTIVATING)
		{
			if (lightFlicker != null && lightFlicker.isFrozen.Value)
				return; // frozen, don't flicker

			if (Timer > 0)
			{
				Timer -= Time.deltaTime;
			}

			if (Timer <= 0)
			{
				// Toggle light on/off
				isLightEnabled.Value = !isLightEnabled.Value;
				Timer = Random.Range(MIN_FLICKER_TIME, MAX_FLICKER_TIME);
			}
		}
	}

	private void TurnLightsOff()
	{
		isLightOn = false;
		isLightEnabled.Value = false; 

		if (playingLightBuzz)
		{
			AudioManager.Instance.Stop3DSoundServerRpc(AudioManager.Instance.Get3DSoundFromList(lightBuzzSFX));
			playingLightBuzz = false;
		}

		lightSource.enabled = false;
		TurnOffMaterialLight();
	}

	private void TurnLightsOn()
	{
		isLightOn = true;
		isLightEnabled.Value = true;
		NetworkObject netObj = GetComponent<NetworkObject>();
		if (!playingLightBuzz && netObj != null && netObj.IsSpawned)
		{
			AudioManager.Instance.Play3DSoundServerRpc(
				AudioManager.Instance.Get3DSoundFromList(lightBuzzSFX),
				lightSource.transform.position,
				false,
				0.9f,
				1f,
				10f,
				false,
				netObj
			);
			playingLightBuzz = true;
		}
		if (playerSeesLight)
		{
			lightSource.enabled = true;
			TurnOnMaterialLight();
		}
	}



}

