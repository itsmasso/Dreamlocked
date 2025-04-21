using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class TeddyBearVisual : MonoBehaviour, IHasDestroyAnimation
{
    [SerializeField] private Animator anim;
    [SerializeField] private Dissolve dissolveScript;

    void Start()
    {
        TeddyBearUseable.onActivateAnimation += PlayActivateAnimation;
        TeddyBearUseable.onDestroyObject += PlayDestroyAnimation;
    }
    
    public void PlayActivateAnimation()
    {
        anim.Play("TeddyBearActivated");
    }
    public void PlayDestroyAnimation()
    {
        dissolveScript.StartDissolvingSkinnedMesh();
        StartCoroutine(DelayedDestroy(dissolveScript.GetDissolveDuration()));
    }
    private IEnumerator DelayedDestroy(float duration)
    {
        yield return new WaitForSeconds(duration);
        Destroy(gameObject);
    }
}
