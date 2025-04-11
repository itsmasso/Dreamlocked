using System;
using System.Collections;
using UnityEngine;

public class LoadingScreenManager : MonoBehaviour
{

    [SerializeField] private GameObject sleepingLoadingScreen;
    [SerializeField] private float fadeSpeed;
    public static event Action onHideSleepingLoadScreen;
    public void ShowSleepingLoadingScreen()
    {
        sleepingLoadingScreen.SetActive(true);
        //sleepingLoadingScreen.GetComponent<FadeUIElements>().FadeInUI(fadeSpeed);
    }
    
    public void HideSleepingLoadingScreen()
    {
        StartCoroutine(HideLoadingScreenAfterDelay());
        //StartCoroutine(FadeOutSleepingLoadScreen());
    }
    private IEnumerator HideLoadingScreenAfterDelay()
    {
        yield return new WaitForSeconds(1.5f);
        sleepingLoadingScreen.SetActive(false);
        onHideSleepingLoadScreen?.Invoke();
    }
    private IEnumerator FadeOutSleepingLoadScreen()
    {
        sleepingLoadingScreen.GetComponent<FadeUIElements>().FadeOutUI(fadeSpeed);
        yield return new WaitForSeconds(fadeSpeed);
        sleepingLoadingScreen.SetActive(false);
    }
}
