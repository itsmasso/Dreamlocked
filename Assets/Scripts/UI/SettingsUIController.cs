using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SettingsUIController : MonoBehaviour
{
    [Header("References")]
    public GameObject settingsPanel;
    public GameObject inGameUI;
    public GameObject crosshair;

    [SerializeField] private GameObject playerObject;
    [Header("Audio Settings")]
    [SerializeField] private Slider volumeSlider;

    private bool isOpen = false;
    private KeybindSettingsManager keybindManager;

    private const string VolumePrefKey = "VolumeLevel"; // Constant key name!

    [SerializeField] private Button muteButton;
    private bool isMuted = false;
    private float lastVolumeBeforeMute = 1f; // remember the last good volume


    void Start()
    {
        keybindManager = FindFirstObjectByType<KeybindSettingsManager>();
        settingsPanel.SetActive(false);

        if (volumeSlider != null)
        {
            // Load saved volume from PlayerPrefs or fallback to 1 
            float savedVolume = PlayerPrefs.GetFloat(VolumePrefKey, 1f);
            volumeSlider.value = savedVolume;
            AudioListener.volume = savedVolume;

            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        if (muteButton != null)
        {
            muteButton.onClick.AddListener(ToggleMute);
        }

    }

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleSettings();
        }
    }

    public void ToggleSettings()
    {
        if (isOpen)
        {
            if (keybindManager != null)
                keybindManager.SaveBindings();
        }

        isOpen = !isOpen;
        settingsPanel.SetActive(isOpen);

        Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isOpen;

        if (inGameUI != null) inGameUI.SetActive(!isOpen);
        if (crosshair != null) crosshair.SetActive(!isOpen);

        if (playerObject != null)
        {
            var input = playerObject.GetComponent<PlayerInput>();
            if (input != null) input.enabled = !isOpen;

            var controller = playerObject.GetComponent<PlayerController>();
            if (controller != null) controller.enabled = !isOpen;
        }

        if (isOpen && keybindManager != null)
            keybindManager.LoadBindings();
    }

    public void OpenSettings()
    {
        crosshair?.SetActive(false);
        inGameUI?.SetActive(false);
        settingsPanel.SetActive(true);
        isOpen = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (playerObject != null)
        {
            var input = playerObject.GetComponent<PlayerInput>();
            if (input != null) input.enabled = false;

            var controller = playerObject.GetComponent<PlayerController>();
            if (controller != null) controller.enabled = false;
        }

        keybindManager?.LoadBindings();
    }

    public void CloseSettings()
    {
        keybindManager?.SaveBindings();

        crosshair?.SetActive(true);
        inGameUI?.SetActive(true);
        settingsPanel.SetActive(false);
        isOpen = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerObject != null)
        {
            var input = playerObject.GetComponent<PlayerInput>();
            if (input != null) input.enabled = true;

            var controller = playerObject.GetComponent<PlayerController>();
            if (controller != null) controller.enabled = true;
        }
    }

    public void OnExitButtonPressed()
    {
        var keybindManager = FindFirstObjectByType<KeybindSettingsManager>();
        if (keybindManager != null)
        {        
            keybindManager?.SaveBindings();
        }

        FindFirstObjectByType<ExitGameManager>()?.ExitToMenu();
    }

    private void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VolumePrefKey, value); // SAVE it immediately
        PlayerPrefs.Save(); // Write to disk
    }

    private void ToggleMute()
    {
        if (!isMuted)
        {
            lastVolumeBeforeMute = AudioListener.volume; // save current volume
            AudioListener.volume = 0f;
            if (volumeSlider != null) volumeSlider.value = 0f;
            isMuted = true;
        }
        else
        {
            AudioListener.volume = lastVolumeBeforeMute;
            if (volumeSlider != null) volumeSlider.value = lastVolumeBeforeMute;
            isMuted = false;
        }

        // Save to PlayerPrefs even for mute!
        PlayerPrefs.SetFloat(VolumePrefKey, AudioListener.volume);
        PlayerPrefs.Save();
    }

}
