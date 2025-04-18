using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeypadScript : MonoBehaviour
{
    [SerializeField] private GameObject keypadUI;
    public static Canvas keypad;
    private int[] currentCode = {0,0,0,0};
    private int arrayIndex = 0;
    private int[] tempCode = {1,9,8,7};
    private static bool unlocked = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        keypad = keypadUI.GetComponentInChildren<Canvas>();
        keypad.enabled = false;
    }

    public static void AccessKeypad()
    {
        keypad.enabled = true;
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }

    public void DisableKeypad()
    {
        keypad.enabled = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public static bool CheckCode()
    {
        return unlocked;
    }

    // These function handle the keypad button clicks
    public void pressKey(int key)
    {
        if (arrayIndex < currentCode.Length)
        {
            currentCode[arrayIndex] = key;
            arrayIndex++;
            //Debug.Log("Code: " + currentCode[0].ToString() + currentCode[1].ToString() + currentCode[2].ToString() + currentCode[3].ToString());
        }
    }
    public void cancel()
    {
        currentCode[0] = 0;
        currentCode[1] = 0;
        currentCode[2] = 0;
        currentCode[3] = 0;
        arrayIndex = 0;
        DisableKeypad();
    }
    public void enter()
    {
        unlocked = CheckCode(currentCode, tempCode);
        currentCode[0] = 0;
        currentCode[1] = 0;
        currentCode[2] = 0;
        currentCode[3] = 0;
        arrayIndex = 0;
        //Debug.Log("Unlocked = " + unlocked);
        DisableKeypad();
    }

    private bool CheckCode(int[] arr1, int[] arr2)
    {
        //Debug.Log("Current Code: " + currentCode[0].ToString() + currentCode[1].ToString() + currentCode[2].ToString() + currentCode[3].ToString());
        //Debug.Log("Temp Code:    " + tempCode[0].ToString() + tempCode[1].ToString() + tempCode[2].ToString() + tempCode[3].ToString());
        if (arr1[0] == arr2[0])
        {
            if(arr1[1] == arr2[1])
            {
                if (arr1[2] == arr2[2])
                {
                    if (arr1[3] == arr2[3])
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

}
