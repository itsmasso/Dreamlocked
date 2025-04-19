using UnityEngine;
using Unity.Netcode;

public class FlashLightUseable : NetworkBehaviour, IUseableItem<ItemData>
{
    [SerializeField] private float batteryLevel;
    public event System.Action<ItemData> OnDataChanged;
    private bool isOn;
    [SerializeField] private Light spotLight;
    [SerializeField] private Material defaultMat, lightMaterial;
    [SerializeField] private Renderer meshRenderer;
    [SerializeField] private float batteryDrainRate;
    [SerializeField] private DetectEnemyInLights interactableLightsScript;
    private bool isDead;
    public ItemData itemData { private set; get; }
    private bool visuallyTurnedOff;
    public override void OnNetworkSpawn()
    {

    }
    void Start()
    {
        isOn = false;
    }
    void Update()
    {
        if (IsServer)
        {
            HandleFlashLightLogic();
            HandleBatteryLife();
        }
    }
    public void InitializeData(ItemData data)
    {
        if (!IsServer) return;
        itemData = data;
        batteryLevel = data.itemCharge;
    }

    private void HandleFlashLightLogic()
    {
        if (isOn && !isDead && batteryLevel > 0f)
        {
            if(visuallyTurnedOff)
            {
                TurnOnRpc();
            }
            interactableLightsScript.DetectEnemiesInFlashLight();
        }
        else
        {
            if(!visuallyTurnedOff)
            {
                TurnOffRpc();
            }
        }
    }
    [Rpc(SendTo.Everyone)]
    private void TurnOnRpc()
    {
        spotLight.enabled = true;
        meshRenderer.materials[2] = lightMaterial;
        visuallyTurnedOff = false;
    }
    [Rpc(SendTo.Everyone)]
    private void TurnOffRpc()
    {
        spotLight.enabled = false;
        meshRenderer.materials[2] = defaultMat;
        visuallyTurnedOff = true;
    }

    private void HandleBatteryLife()
    {
        if (!IsServer || itemData.id == -1 || isDead || !isOn) return;

        batteryLevel -= batteryDrainRate * Time.deltaTime;

        if (batteryLevel <= 0f)
        {
            batteryLevel = 0f;
            AllSeeFlashlightDeadRpc();
        }

        var data = itemData;
        data.itemCharge = batteryLevel;
        itemData = data;

        OnDataChanged?.Invoke(itemData);
    }

    [Rpc(SendTo.Everyone)]
    private void AllSeeFlashlightDeadRpc()
    {
        isDead = true;
    }

    public void UseItem()
    {
        AllSeeFlashlightSwitchRpc();
    }
    public ItemData GetData()
    {
        return itemData;
    }

    [Rpc(SendTo.Everyone)]
    private void AllSeeFlashlightSwitchRpc()
    {
        isOn = !isOn;
    }
}

