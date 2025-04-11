using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;



public class TransitionManager : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject fadeBackground;
    [SerializeField] private Volume volume;
    private DepthOfField depthOfField;
    
    void Awake()
    {
        fadeBackground.SetActive(false);

    }
    void Start()
    {
        GameManager.Instance.onNextLevel += StartFadeInTransition;
        LoadingScreenManager.onHideSleepingLoadScreen += StartFadeOutTransition;
        DepthOfField tmp;
        if (volume.profile.TryGet(out tmp))
        {
            depthOfField = tmp;
          
        }
        
    }

    public void StartFadeInTransition()
    {
        
        StartCoroutine(FadeInTransition());
    }
    
    private IEnumerator FadeInTransition()
    {
        fadeBackground.SetActive(true);
        animator.Play("FadeIn");
        depthOfField.active = true;

        AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
        float animationTime = clipInfo.Length > 0 ? clipInfo[0].clip.length : 1.0f; // Default to 1s if not found
        StartCoroutine(BlurIn(animationTime));
        yield return new WaitForSeconds(animationTime-0.1f);
        depthOfField.active = false;
        fadeBackground.SetActive(false);
    }
    
    private IEnumerator BlurIn(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            depthOfField.focalLength.value = Mathf.Lerp(1f, 300f, t); // Adjust range based on your effect
            yield return null;
        }

        depthOfField.focalLength.value = 300f; // Ensure it ends exactly at 1
    }
    
    private IEnumerator BlurOut(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            depthOfField.focalLength.value = Mathf.Lerp(300f, 1, t); // Adjust range based on your effect
            yield return null;
        }

        depthOfField.focalLength.value = 1f; // Ensure it ends exactly at 1
    }
    
    public void StartFadeOutTransition()
    {
       StartCoroutine(FadeOutAnimation());
        
    }
    
    private IEnumerator FadeOutAnimation()
    {
        fadeBackground.SetActive(true);
        animator.Play("FadeOut");
        depthOfField.active = true;

        AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
        float animationTime = clipInfo.Length > 0 ? clipInfo[0].clip.length : 1.0f; // Default to 1s if not found
        StartCoroutine(BlurOut(animationTime));
        yield return new WaitForSeconds(animationTime-0.1f);
        depthOfField.active = false;
        fadeBackground.SetActive(false);
    }
    void OnDestroy()
    {
        GameManager.Instance.onNextLevel -= StartFadeInTransition;
        LoadingScreenManager.onHideSleepingLoadScreen -= StartFadeOutTransition;
    }

}
