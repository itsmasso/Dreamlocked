using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FadeUIElements : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;

    void Start()
    {
        canvasGroup.alpha = 1f;
    }
    
    public void FadeInUI(float duration)
    {
        StartCoroutine(FadeElements(duration, 0f, 1f));
    }
    public void FadeOutUI(float duration)
    {
        StartCoroutine(FadeElements(duration, 1f, 0f));
    }

    private IEnumerator FadeElements(float duration, float start, float finish)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Clamp01(Mathf.Lerp(start, finish, t));
            yield return null;
        }
        
        
    }


}
