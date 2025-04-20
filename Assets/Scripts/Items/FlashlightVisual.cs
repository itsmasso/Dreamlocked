using UnityEngine;

public class FlashlightVisual : MonoBehaviour
{
    [SerializeField] private Renderer meshRenderer;
    [SerializeField] private Material defaultMat, lightMaterial;

    void Start()
    {
        FlashLightUseable.onFlashLightToggle += ConfigureFlashLight;
    }
    
    public void ConfigureFlashLight(bool turnOn)
    {
        if(turnOn)
        {
            meshRenderer.materials[2] = lightMaterial;
        }else
        {
            meshRenderer.materials[2] = defaultMat;
        }
    }

    void OnDestroy()
    {
        FlashLightUseable.onFlashLightToggle -= ConfigureFlashLight;
    }
}
