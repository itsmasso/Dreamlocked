
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHotbarUI : MonoBehaviour
{
    [SerializeField] private Image[] slotSprites;
    [SerializeField] List<RectTransform> hotbarSlots = new List<RectTransform>();
    [SerializeField] private GameObject[] slotChargeBars;
    private Slider[] chargeBarSliders;
    [SerializeField] private Sprite emptySlotImage;
    [SerializeField] private float scaleSpeed = 2f;
    [SerializeField] private float scaleAmount = 1.3f; // 1.1 = 10% bigger
    [SerializeField] private ItemScriptableObject flashlight;
    private int currentSelectedSlot;
    [SerializeField]private Vector2[] originalSizes;
    void Start()
    {
        PlayerInventory.onNewSlotSelected += NewSlotSelected;
        PlayerInventory.onAddSprite += AddSelectedSlotSprite;
        PlayerInventory.onRemoveSprite += RemoveSelectedSlotSprite;
        PlayerInventory.onUpdateChargeBar += UpdateChargeBar;
        GameManager.Instance.onNextLevel += ResetHotbar;
        originalSizes = new Vector2[hotbarSlots.Count];
        for (int i = 0; i < hotbarSlots.Count; i++)
        {

            originalSizes[i] = hotbarSlots[i].sizeDelta;
            slotSprites[i].sprite = null;
        }
        
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
        currentSelectedSlot = slotNumber;
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
            slotSprites[slotNumber].color = new Color(1, 1, 1, 1);
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
             slotSprites[slotNumber].color = new Color(1, 1, 1, 0);
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
                //Debug.Log($"[Hotbar UI] Slot {slotNumber} Slider reference ID: {chargeBarSliders[slotNumber].GetInstanceID()}, name: {chargeBarSliders[slotNumber].name}");
            }
        }
    }
    public void ResetHotbar()
    {
        for (int i = 0; i < slotSprites.Length; i++)
        {
             slotSprites[i].sprite = null;
             slotSprites[i].color = new Color(1, 1, 1, 0);
            slotChargeBars[i].SetActive(false);

            if (chargeBarSliders[i] != null)
            {
                chargeBarSliders[i].value = 0f;
            }
        }

        NewSlotSelected(0);
    }

    void Update()
    {
        for (int i = 0; i < hotbarSlots.Count; i++)
        {
            Vector2 targetSize = (i == currentSelectedSlot) ? originalSizes[i] * scaleAmount : originalSizes[i];
            hotbarSlots[i].sizeDelta = Vector2.Lerp(hotbarSlots[i].sizeDelta, targetSize, Time.deltaTime * scaleSpeed);
        }
        
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
