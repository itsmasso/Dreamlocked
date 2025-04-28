using UnityEngine;

public class AudioFollowTarget : MonoBehaviour
{
    public Transform target; // object to follow
    public Vector3 offset = Vector3.zero; // optional positional offset

    private void Update()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }

    public void SetTarget(Transform newTarget, Vector3 newOffset)
    {
        target = newTarget;
        offset = newOffset;
    }
}
