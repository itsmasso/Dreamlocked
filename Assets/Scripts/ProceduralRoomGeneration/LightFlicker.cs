using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class LightFlicker : NetworkBehaviour
{
    [SerializeField] private float freezeAttemptInterval = 5f;
    [Tooltip("How long lights stay frozen.")]
    [SerializeField] private float freezeDuration = 1f;
    private float timer;
    public NetworkVariable<bool> isFrozen = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private bool hasPlayedLightFlicker;
    [SerializeField] private Sound2DSO lightFlickerSFX;
    [SerializeField] private Sound2DSO lightsOutSFX;
    [SerializeField] private Sound2DSO lightsOnSFX;
    private bool hasPlayedLightsOutSound = false;
    private bool hasPlayedLightsOnSound = true;
    void Start()
    {
        hasPlayedLightFlicker = false;
        hasPlayedLightsOutSound = false;
        hasPlayedLightsOnSound = true;
    }

    private void PlayLightsOutSFX()
    {
        if (!hasPlayedLightsOutSound && GFClockManager.Instance != null)
        {
            if (GFClockManager.Instance.GetMQThreatLevel() == MQThreatLevel.AWAKENED)
            {
                hasPlayedLightsOnSound = false;
                hasPlayedLightsOutSound = true;
                AudioManager.Instance.Stop2DSound(lightFlickerSFX, 0f);

                AudioManager.Instance.Play2DSound(lightsOutSFX, 0f, true);
            }
        }

    }

    private void PlayLightsOnSFX()
    {
        if (!hasPlayedLightsOnSound && GFClockManager.Instance != null)
        {
            if (GFClockManager.Instance.GetMQThreatLevel() == MQThreatLevel.PASSIVE)
            {
                hasPlayedLightsOutSound = false;
                hasPlayedLightsOnSound = true;
                AudioManager.Instance.Stop2DSound(lightFlickerSFX, 0f);
                AudioManager.Instance.Play2DSound(lightsOnSFX, 0f, true);
            }
        }

    }
    void Update()
    {

        if (GFClockManager.Instance != null)
        {
            if (GFClockManager.Instance.GetMQThreatLevel() == MQThreatLevel.ACTIVATING)
            {
                timer -= Time.deltaTime;
                if (!hasPlayedLightFlicker)
                {
                    AudioManager.Instance.Play2DSound(lightFlickerSFX, 0f);
                    hasPlayedLightFlicker = true;
                }
                if (timer <= 0f)
                {
                    timer = freezeAttemptInterval + Random.Range(-3f, 3f); // Make it a little random

                    if (Random.value < 0.25f) // 25% chance to actually freeze
                    {
                        StartCoroutine(FreezeFlickering());
                    }
                }

            }

            if (isFrozen.Value || GFClockManager.Instance.GetMQThreatLevel() == MQThreatLevel.AWAKENED)
            {
                if (hasPlayedLightFlicker)
                {
                    AudioManager.Instance.Stop2DSound(lightFlickerSFX, 0f);
                    hasPlayedLightFlicker = false;
                }

            }

        }
        PlayLightsOutSFX();
        PlayLightsOnSFX();

    }
    private IEnumerator FreezeFlickering()
    {
        if (IsServer)
        {
            isFrozen.Value = true;
            yield return new WaitForSeconds(freezeDuration);
            isFrozen.Value = false;
        }
        if (hasPlayedLightFlicker)
        {
            AudioManager.Instance.Stop2DSound(lightFlickerSFX, 0f);
            hasPlayedLightFlicker = false;
        }
    }

}
