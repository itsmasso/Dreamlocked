
/*****************************************************************
 * GFClockManager Script
 *****************************************************************
 * Author: Dylan Werelius
 *****************************************************************
 * Description:
    The purpose of this script is to act as a controller for the
    GFClock which helps control the Mannequin monsters. This 
    script is a "timer" script which counts down the seconds until
    the GFClock starts to chime. This
    script does not directly control the Mannequin monsters, but
    the actions of the Mannequin monsters are based on the enum
    MQThreatLevel (Mannequin Threat Level). This script only tells
    them when they can be active and roaming.
 *****************************************************************/
using UnityEngine;
using Unity.Netcode;
// This enum will handle what stage the timer is at
public enum MQThreatLevel
{
    PASSIVE,
    ACTIVATING,
    AWAKENED
}

public class GFClockManager : NetworkSingleton<GFClockManager>, IInteractable
{
    private const float TOTAL_TIME = 180;
    private const float TIME_TO_DANGER = 20;
    private const float EXTRACTION_TIME = 60;
    private const float REWIND_BUFFER = TOTAL_TIME - 10;
    private bool gameEnding = false;
    private float currentTime;
    private bool timeRunning;
    public NetworkVariable<MQThreatLevel> currentThreatLevel = new NetworkVariable<MQThreatLevel>(MQThreatLevel.PASSIVE, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Developer Variables
    private bool printedActivating = false;
    private bool printedAwakened = false;
    protected override void Awake()
	{
		base.Awake();
	}
    void Start()
    {
        currentTime = TOTAL_TIME;
        timeRunning = true;
        Debug.Log("Clock Started");
    }

    // Update is called once per frame
    void Update()
    {
        if(!IsServer) return;
        
        if(!gameEnding)
        {
            Timer();
        } else {
            ExtractionTimer();
        }
        
    }

    /*****************************************************************
    * InstantResetTimer
    *****************************************************************
    * Author: Dylan Werelius
    *****************************************************************
    * Description:
        This function will reset the timer back to the TOTAL_TIME and
        start it.
    *****************************************************************/
    private void InstantResetTimer()
    {
        currentTime = TOTAL_TIME;
        timeRunning = true;
        printedActivating = false;
        printedAwakened = false;
    }

    public MQThreatLevel GetMQThreatLevel()
    {
        return currentThreatLevel.Value;
    }
    
    private void Timer()
    {
        // Decrement the timer by one second if the clock is running and the currentTime > -1
        currentTime = (timeRunning && currentTime > -1) ? currentTime -= Time.deltaTime : currentTime;

        // This will interpret the timer and update the level of danger based on how much time is left
        if (timeRunning && currentTime <= 0)
        {
            // At this time, the GF Clock stops chiming and the lights go out
            currentThreatLevel.Value = MQThreatLevel.AWAKENED;
            timeRunning = false;
            
            // This is just for the sake of making sure the timer is running
            if (!printedAwakened)
            {
                Debug.Log("Monsters have Awakened");
                printedAwakened = true;
            }
            //Debug.Log("Current Time: " + currentTime);
        } 
        else if (timeRunning && currentTime <= TIME_TO_DANGER) 
        {
            // At this time, the GF Clock will start to chime and lights begin to flicker
            currentThreatLevel.Value = MQThreatLevel.ACTIVATING;

            // This is just for the sake of making sure the timer is running
            if (!printedActivating)
            {
                Debug.Log("Monsters are Activating");
                printedActivating = true;
            }
            //Debug.Log("Current Time: " + currentTime);
        } else if (timeRunning) 
        {
            currentThreatLevel.Value = MQThreatLevel.PASSIVE;
        }
        
    }

    private void ExtractionTimer()
    {
        currentThreatLevel.Value = MQThreatLevel.ACTIVATING;

        // Count down the timer
        currentTime = (timeRunning && currentTime > 0) ? currentTime -= Time.deltaTime : 0;

        if (currentTime <= 0)
        {
            timeRunning = false;
            //Debug.Log("Extraction Complete");
        }
    }
    public void StartExtraction()
    {
        gameEnding = true;
        currentTime = EXTRACTION_TIME;
        timeRunning = true;
    }

    public void Interact(NetworkObjectReference playerObject)
    {
       
        if(!gameEnding && currentTime <= REWIND_BUFFER){
         Debug.Log("interacting");
            InstantResetTimer();
        }
    }
}
