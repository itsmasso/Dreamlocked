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
        if (anim != null)
        {
            Debug.Log("play animation");
            anim.Play("TeddyBearActivated");
        }
    }
    public void PlayDestroyAnimation()
    {
        if (dissolveScript != null)
        {
            dissolveScript.StartDissolvingSkinnedMesh();
            PlayerInventory playerInventory = GetComponentInParent<PlayerInventory>();
            if (playerInventory != null)
            {
                playerInventory.SetCurrentVisualItemNull();
            }
        }
    }


}
