using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class KeybindItem : MonoBehaviour
{
    public TMP_Text labelText;
    public TMP_Text keyText;
    public Button rebindButton;

    private InputAction action;

    public void Init(string label, InputAction inputAction)
    {
        labelText.text = label;
        action = inputAction;
        keyText.text = action.bindings[0].ToDisplayString();

        rebindButton.onClick.AddListener(() =>
        {
            KeybindSettingsManager.IsRebindingKey = true;
            StartRebinding();
        });
    }

    private void StartRebinding()
    {
        keyText.text = "...";
        action.Disable();

        action.PerformInteractiveRebinding()
            .WithControlsExcluding("Mouse")
            .OnComplete(operation =>
            {
                KeybindSettingsManager.IsRebindingKey = false;
                keyText.text = action.bindings[0].ToDisplayString();
                action.Enable();
                operation.Dispose();

                EventSystem.current.SetSelectedGameObject(null); // Clear UI focus
            })
            .Start();
    }
}
