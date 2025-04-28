using System.Collections;
using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    [SerializeField] private float freezeAttemptInterval = 5f;
    [Tooltip("How long lights stay frozen.")]
    [SerializeField] private float freezeDuration = 1f;
    private float timer;
    private bool isFrozen = false;
    public bool IsFrozen => isFrozen;
    private bool hasPlayedLightFlicker;
    [SerializeField] private Sound2DSO lightFlickerSFX;
    [SerializeField] private Sound2DSO lightsOutSFX;
    [SerializeField] private Sound2DSO lightsOnSFX;

    void Start()
    {
        hasPlayedLightFlicker = false;
        if (GFClockManager.Instance != null)
        {
            GFClockManager.Instance.onAwakened += PlayLightsOutSFX;
            GFClockManager.Instance.onPassive += PlayLightsOnSFX;
        }
    }

    private void PlayLightsOutSFX()
    {
        AudioManager.Instance.Stop2DSound(lightFlickerSFX, 0f);
        AudioManager.Instance.Play2DSound(lightsOutSFX, 0f, true);
    }

    private void PlayLightsOnSFX()
    {
        AudioManager.Instance.Play2DSound(lightsOnSFX, 0f ,true);
    }
    void Update()
    {

        if (GFClockManager.Instance != null && GFClockManager.Instance.GetMQThreatLevel() == MQThreatLevel.ACTIVATING)
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


            if (isFrozen || GFClockManager.Instance.GetMQThreatLevel() == MQThreatLevel.AWAKENED)
            {
                if (hasPlayedLightFlicker)
                {
                    AudioManager.Instance.Stop2DSound(lightFlickerSFX, 0f);
                    hasPlayedLightFlicker = false;
                }

            }

        }

    }
    private IEnumerator FreezeFlickering()
    {
        isFrozen = true;
        yield return new WaitForSeconds(freezeDuration);
        isFrozen = false;
    }
    void OnDestroy()
    {
        if (GFClockManager.Instance != null)
        {
            GFClockManager.Instance.onAwakened -= PlayLightsOnSFX;
            GFClockManager.Instance.onPassive -= PlayLightsOutSFX;
        }
    }
}
