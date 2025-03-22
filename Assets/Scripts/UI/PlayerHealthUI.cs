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
        PlayerHealth.onTakeDamage += UpdateHealthBar;
        maxHealth = playerScriptable.health;
        UpdateHealthBar(maxHealth); //temp
        //add code for udpating health bar betweens scenes
    }
    
    private void UpdateHealthBar(int currentHealth)
    {
        healthBar.value = (float)currentHealth/maxHealth; 
        if(currentHealth <= 0) healthBar.value = 0;
        
    }

    public override void OnDestroy()
    {
        PlayerHealth.onTakeDamage -= UpdateHealthBar;
    }
}
