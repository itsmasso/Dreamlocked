using System;
using TMPro;
using UnityEngine;

public class DisplayPlayerProperties : MonoBehaviour
{
    //not to be shown in builds, just for testing
    [SerializeField] private TMP_Text playerSpeedText, playerVelocityText;
    [SerializeField] private GameObject playerPropertyDisplay;
    public static event Action<bool> onEnableDebugging;
    [SerializeField] private bool debuggingOn;
    private void Start(){
        PlayerMovement.onUpdateStats += SetPlayerPropertyText;
    }

    private void SetPlayerPropertyText(PlayerDebugStats playerStats){
        playerSpeedText.text = string.Format("Player Speed: {0}", playerStats.playerSpeed);
        playerVelocityText.text = string.Format("Player Velocity: {0}", playerStats.playerVelocity);
    }
    private void Update(){
        if(Input.GetKeyDown(KeyCode.BackQuote)){
            debuggingOn = !debuggingOn;
            onEnableDebugging?.Invoke(debuggingOn);
        }

        if(debuggingOn){
            playerPropertyDisplay.SetActive(true);
        }else{
            playerPropertyDisplay.SetActive(false);
        }
    }
    private void OnDestroy() {
        PlayerMovement.onUpdateStats -= SetPlayerPropertyText;
    }
}
