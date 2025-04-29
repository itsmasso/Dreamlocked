using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;

public class KeypadScript : MonoBehaviour
{
    [SerializeField] private GameObject keypadUI;
    public static Canvas keypad;
    private int[] currentCode = {0,0,0,0};
    private static int[] transmittedCode = {0,0,0,0};
    private int arrayIndex = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        keypad = keypadUI.GetComponentInChildren<Canvas>();
        keypad.enabled = false;
        ResetCode(currentCode);
        ResetCode(transmittedCode);
        arrayIndex = 0;
    }

    public static void AccessKeypad()
    {
        keypad.enabled = true;
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }

    public static int[] GetEnteredCode()
    {
        return transmittedCode;
    }

    public void DisableKeypad()
    {
        keypad.enabled = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        EventSystem.current.SetSelectedGameObject(null);
    }

    // These function handle the keypad button clicks
    public void pressKey(int key)
    {
        if (arrayIndex < currentCode.Length)
        {
            currentCode[arrayIndex] = key;
            arrayIndex++;
            AudioManager.Instance.PlayLocalClientOnly2DSound(AudioManager.Instance.Get2DSound("KeypadBeep"), 0f, true);
            //Debug.Log("Code: " + currentCode[0].ToString() + currentCode[1].ToString() + currentCode[2].ToString() + currentCode[3].ToString());
        }
    }
    public void cancel()
    {
        ResetCode(currentCode);
        ResetCode(transmittedCode);
        arrayIndex = 0;
        DisableKeypad();
    }
    public void enter()
    {
        SetTransmittedCode();
        arrayIndex = 0;
        ResetCode(currentCode);
        DisableKeypad();
    }

    private void ResetCode(int[] arr)
    {
        for (int index = 0; index < currentCode.Length; index++)
        {
            arr[index] = 0;
        }
    }

    private void SetTransmittedCode()
    {
        for (int index = 0; index < currentCode.Length; index++)
        {
            transmittedCode[index] = currentCode[index];
        }
    }

}
