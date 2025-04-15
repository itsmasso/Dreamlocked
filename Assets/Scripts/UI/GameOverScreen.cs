using System.Collections;

using UnityEngine;
using UnityEngine.UI;

public class GameOverScreen : MonoBehaviour
{
    [SerializeField] private float fadeDuration;
    private int opacity = Shader.PropertyToID("_Opacity");
    [SerializeField] private Image tvStaticScreen;
    [SerializeField] private FadeUIElements uiElementFader;

    void OnEnable()
    {
        tvStaticScreen.material.SetFloat(opacity, 0f);
        uiElementFader.FadeInUI(fadeDuration);
        StartCoroutine(StartScreenFade(0f, 1f));
    }

    private IEnumerator StartScreenFade(float startAmount, float finishAmount)
    {
        float elapsedtime = 0;
        while(elapsedtime < fadeDuration)
        {
            elapsedtime += Time.deltaTime;
            
            float lerpedAmount = Mathf.Lerp(startAmount, finishAmount, elapsedtime/fadeDuration);
            tvStaticScreen.material.SetFloat(opacity, lerpedAmount);
            yield return null;
        }
        
        tvStaticScreen.material.SetFloat(opacity, finishAmount);
    }

    void OnDisable()
    {
        tvStaticScreen.material.SetFloat(opacity, 0f);
    }
}
