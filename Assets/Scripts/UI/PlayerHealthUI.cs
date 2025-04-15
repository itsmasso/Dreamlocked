using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
public class PlayerHealthUI : NetworkBehaviour
{
    [SerializeField] private Slider healthBar;
    [SerializeField] private PlayerScriptable playerScriptable;
    private int maxHealth;

    void Start()
    {
        PlayerHealth.onUpdateHealth += UpdateHealthBar;
        maxHealth = playerScriptable.health;
        UpdateHealthBar(maxHealth); 
    }

    private void UpdateHealthBar(int currentHealth)
    {
        float val = Mathf.Clamp01((float)currentHealth / maxHealth);

        //Debug.Log($"HealthBar Value: {val}");
        healthBar.value = val;
        if (currentHealth <= 0) healthBar.value = 0;

    }

    public override void OnDestroy()
    {
        PlayerHealth.onUpdateHealth -= UpdateHealthBar;
    }
}
