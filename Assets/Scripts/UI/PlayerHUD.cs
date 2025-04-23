using Unity.Netcode;
using UnityEngine;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private GameObject playerUI;

    private void HideUI()
    {
        playerUI.SetActive(false);
    }
    private void ShowUI()
    {
        playerUI.SetActive(true);
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
