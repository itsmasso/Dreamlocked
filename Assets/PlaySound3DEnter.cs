using UnityEngine;
using Unity.Netcode;
using System.Collections;
public class PlaySound3DEnter : StateMachineBehaviour
{
    [SerializeField] private Sound3DSO sound;
    [SerializeField] private bool isOneShot, preventDupes;
    [SerializeField] private float volume, minDistance, maxDistance;
    [SerializeField] private float delay;
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        
       AudioManager.Instance.Play3DServerSoundDelayed(delay, AudioManager.Instance.Get3DSoundFromList(sound), animator.transform.position, isOneShot, volume, minDistance, maxDistance, preventDupes, animator.GetComponent<NetworkObject>());
    }

}
