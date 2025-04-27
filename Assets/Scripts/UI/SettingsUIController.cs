using UnityEngine;
using UnityEngine.InputSystem;

public class SettingsUIController : MonoBehaviour
{
    [Header("References")]
    public GameObject settingsPanel;
    public GameObject inGameUI;
    public GameObject crosshair;

    [SerializeField] private GameObject playerObject;

    private bool isOpen = false;
    private KeybindSettingsManager keybindManager;

    void Start()
    {
        keybindManager = FindFirstObjectByType<KeybindSettingsManager>();
        settingsPanel.SetActive(false);
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
            // Save keybinds on close
            if (keybindManager != null)
                keybindManager.SaveBindings();
        }

        isOpen = !isOpen;
        settingsPanel.SetActive(isOpen);

        // Cursor control
        Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isOpen;

        // Toggle UI and crosshair
        if (inGameUI != null) inGameUI.SetActive(!isOpen);
        if (crosshair != null) crosshair.SetActive(!isOpen);

        // Disable player control
        if (playerObject != null)
        {
            var input = playerObject.GetComponent<PlayerInput>();
            if (input != null)
                input.enabled = !isOpen;

            var controller = playerObject.GetComponent<PlayerController>();
            if (controller != null)
                controller.enabled = !isOpen;
        }

        // Load keybinds when opening
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
        // Save keybinds before exiting
        if (keybindManager != null)
        {        
            keybindManager?.SaveBindings();
        }

        FindFirstObjectByType<ExitGameManager>()?.ExitToMenu();
    }
}
