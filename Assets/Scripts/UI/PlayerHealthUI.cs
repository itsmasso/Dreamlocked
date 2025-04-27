using TMPro;
using Unity.Netcode;
using UnityEngine;

using UnityEngine.UI;
public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private Slider healthBar;
    [SerializeField] private PlayerScriptable playerScriptable;
    private int maxHealth;
    [SerializeField] private TextMeshProUGUI healthNumber;
    
 
    void Start()
    {
        PlayerHealth.onUpdateHealth += UpdateHealthBar;
        maxHealth = playerScriptable.health;
        UpdateHealthBar(maxHealth);
    }

    private void UpdateHealthBar(int currentHealth)
    {
        float val = Mathf.Clamp01((float)currentHealth / maxHealth);
        
        healthBar.value = val;
        if (currentHealth <= 0) healthBar.value = 0;
        healthNumber.text = currentHealth.ToString();
        
    }

    public void OnDestroy()
    {
        PlayerHealth.onUpdateHealth -= UpdateHealthBar;
    }
}
