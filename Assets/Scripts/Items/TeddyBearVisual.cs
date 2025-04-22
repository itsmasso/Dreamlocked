using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class TeddyBearVisual : MonoBehaviour, IHasDestroyAnimation
{
    [SerializeField] private Animator anim;
    [SerializeField] private Dissolve dissolveScript;

    void Start()
    {
        TeddyBearUseable.onPlayAnimation += PlayAnimation;
    }
    
    public void PlayDestroyAnimation()
    {
        if (dissolveScript != null)
        {
            dissolveScript.StartDissolvingSkinnedMesh();
            StartCoroutine(DelayedDestroy());
            
        }
    }
    
    public void PlayAnimation(bool canActivate)
    {
        anim?.SetBool("Activate", canActivate);
    }
    private IEnumerator DelayedDestroy()
    {
        yield return new WaitForSeconds(dissolveScript.GetDissolveDuration());
        Destroy(gameObject);
    }
    void OnDestroy()
    {
        TeddyBearUseable.onPlayAnimation -= PlayAnimation;
    }
    

}
