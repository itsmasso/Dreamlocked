
using UnityEngine;
using UnityEngine.UI;

public class PlayerHotbarUI : MonoBehaviour
{
    [SerializeField] private Image[] slotSprites;
    [SerializeField] private GameObject[] highlightSlots;
    [SerializeField] private ItemListScriptableObject itemScriptableObjList;
    void Start()
    {
        PlayerInventory.onNewSlotSelected += NewSlotSelected;
        PlayerInventory.onAddItem += AddSelectedSlotSprite;
        PlayerInventory.onDropItem += RemoveSelectedSlotSprite;
        NewSlotSelected(0);
    }

    private void NewSlotSelected(int slotNumber)
    {
        for (int i = 0; i < highlightSlots.Length; i++)
        {
            highlightSlots[i].SetActive(i == slotNumber); // Only set the selected one to true
        }
    }

    private void AddSelectedSlotSprite(int slotNumber, int itemSOIndex)
    {
        // Set sprite only if the slot is valid and sprite is not null
        if (slotNumber >= 0 && slotNumber < slotSprites.Length)
        {
            slotSprites[slotNumber].sprite = itemScriptableObjList.itemListSO[itemSOIndex].itemSprite;
        }
    }

    private void RemoveSelectedSlotSprite(int slotNumber)
    {
        if (slotNumber >= 0 && slotNumber < slotSprites.Length)
        {
            slotSprites[slotNumber].sprite = null;
        }
    }

    void OnDestroy()
    {
        PlayerInventory.onNewSlotSelected -= NewSlotSelected;
        PlayerInventory.onAddItem -= AddSelectedSlotSprite;
        PlayerInventory.onDropItem -= RemoveSelectedSlotSprite;
    }

}
