using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private GameObject playerUI;
    [SerializeField] private TextMeshProUGUI currentLevelText;
    private void HideUI()
    {
        playerUI.SetActive(false);
    }
    private void ShowUI()
    {
        playerUI.SetActive(true);
    }
    void Update()
    {
        currentLevelText.text = $"{GameManager.Instance.GetCurrentDreamLayer()} Layers Deep";
    }
    void Start()
    {
        GameManager.Instance.onGameStart += ShowUI;
    
       PlayerHealth.onDeath += HideUI;
    }
    void OnDestroy()
    {
        GameManager.Instance.onGameStart -= ShowUI;
       PlayerHealth.onDeath -= HideUI;
    }

}
