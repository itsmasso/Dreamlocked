using UnityEngine;
[RequireComponent(typeof(Light))]
public class MenuSceneLightFlicker : MonoBehaviour
{
    private Light myLight;
    private float baseIntensity;

    [Header("Flicker Settings")]
    public float flickerSpeed = 0.1f; // How fast it flickers
    public float intensityVariation = 0.5f; // How much it varies

    private void Start()
    {
        myLight = GetComponent<Light>();
        baseIntensity = myLight.intensity;
        InvokeRepeating(nameof(Flicker), 0f, flickerSpeed);
    }

    private void Flicker()
    {
        myLight.intensity = baseIntensity + Random.Range(-intensityVariation, intensityVariation);
    }
}
