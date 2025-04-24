
using UnityEngine;
using UnityEngine.UI;

public class PlayerHotbarUI : MonoBehaviour
{
    [SerializeField] private Image[] slotSprites;
    [SerializeField] private GameObject[] highlightSlots;
    [SerializeField] private GameObject[] slotChargeBars;
    private Slider[] chargeBarSliders;

    [SerializeField] private ItemScriptableObject flashlight;
    void Start()
    {
        PlayerInventory.onNewSlotSelected += NewSlotSelected;
        PlayerInventory.onAddSprite += AddSelectedSlotSprite;
        PlayerInventory.onRemoveSprite += RemoveSelectedSlotSprite;
        PlayerInventory.onUpdateChargeBar += UpdateChargeBar;
        GameManager.Instance.onNextLevel += ResetHotbar;

        foreach (GameObject chargeBar in slotChargeBars)
        {
            chargeBar.gameObject.SetActive(false);
        }
        chargeBarSliders = new Slider[slotChargeBars.Length];
        for (int i = 0; i < slotChargeBars.Length; i++)
        {
            chargeBarSliders[i] = slotChargeBars[i].GetComponentInChildren<Slider>();
        }
        NewSlotSelected(0);
    }

    private void NewSlotSelected(int slotNumber)
    {
        for (int i = 0; i < highlightSlots.Length; i++)
        {
            highlightSlots[i].SetActive(i == slotNumber); // Only set the selected one to true
        }
    }

    private void AddSelectedSlotSprite(int slotNumber, int id, ItemData itemData)
    {

        if (slotNumber >= 0 && slotNumber < slotSprites.Length)
        {
            var item = ItemDatabase.Get(id);
            if (item == null)
            {
                Debug.LogError($"ItemDatabase returned null for id: {id}");
                return;
            }

            slotSprites[slotNumber].sprite = item.icon;
            //hard code this in for now but later on might want to add an item type
            if (id == flashlight.id)
            {
                slotChargeBars[slotNumber].gameObject.SetActive(true);
                chargeBarSliders[slotNumber].value = Mathf.Clamp01(itemData.itemCharge / 100);
            }
        }
    }

    private void RemoveSelectedSlotSprite(int slotNumber)
    {
        if (slotNumber >= 0 && slotNumber < slotSprites.Length)
        {
            slotSprites[slotNumber].sprite = null;
            slotChargeBars[slotNumber].gameObject.SetActive(false);
        }
    }

    private void UpdateChargeBar(int slotNumber, float chargeAmount)
    {
        if (slotNumber >= 0 && slotNumber < slotChargeBars.Length)
        {
            if (slotChargeBars[slotNumber] != null)
            {
                chargeBarSliders[slotNumber].value = Mathf.Clamp01(chargeAmount / 100);
                Debug.Log($"[Hotbar UI] Slot {slotNumber} Slider reference ID: {chargeBarSliders[slotNumber].GetInstanceID()}, name: {chargeBarSliders[slotNumber].name}");
            }
        }
    }
    public void ResetHotbar()
    {
        for (int i = 0; i < slotSprites.Length; i++)
        {
            slotSprites[i].sprite = null;
            highlightSlots[i].SetActive(false);
            slotChargeBars[i].SetActive(false);

            if (chargeBarSliders[i] != null)
            {
                chargeBarSliders[i].value = 0f;
            }
        }

        NewSlotSelected(0);
    }
    void OnDestroy()
    {
        PlayerInventory.onNewSlotSelected -= NewSlotSelected;
        PlayerInventory.onAddSprite -= AddSelectedSlotSprite;
        PlayerInventory.onRemoveSprite -= RemoveSelectedSlotSprite;
        PlayerInventory.onUpdateChargeBar -= UpdateChargeBar;
        GameManager.Instance.onNextLevel -= ResetHotbar;
    }

}
