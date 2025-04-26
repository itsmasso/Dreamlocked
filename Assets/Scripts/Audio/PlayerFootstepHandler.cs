using Unity.Netcode;
using UnityEngine;

public class PlayerFootstepHandler : NetworkBehaviour
{
    [SerializeField] private Sound3DSO[] footstepSounds;
    [SerializeField] private float walkingFootStepInterval = 1f;
    [SerializeField] private AudioSource footStepAudioSource;
    private PlayerController playerController;
    private float footstepTimer = 0f;

    void Start()
    {
        playerController = GetComponent<PlayerController>();
        footStepAudioSource = GetComponent<AudioSource>();
        
    }

    void Update()
    {
        if (IsOwner && playerController.isGrounded && playerController.inputDir != Vector2.zero)
        {
            footstepTimer -= Time.deltaTime;

            if (footstepTimer <= 0f)
            {
                PlayFootstepRpc();
                footstepTimer = walkingFootStepInterval / playerController.moveSpeed; // Reset timer
            }
        }
        else
        {
             footstepTimer = walkingFootStepInterval / playerController.moveSpeed;
        }
    }

    [Rpc(SendTo.Everyone)]
    private void PlayFootstepRpc()
    {
        if (footstepSounds.Length == 0) return;

        Sound3DSO footStep = footstepSounds[Random.Range(0, footstepSounds.Length)];
        footStepAudioSource.pitch = Random.Range(0.9f, 1.1f);
        footStepAudioSource.volume = Random.Range(Mathf.Clamp01(footStep.volume - 0.4f), footStep.volume);
        footStepAudioSource.minDistance = footStep.minDistance;
        footStepAudioSource.maxDistance = footStep.maxDistance;
        footStepAudioSource.outputAudioMixerGroup = footStep.audioMixerGroup;
        footStepAudioSource.PlayOneShot(footStep.clip, footStep.volume);
       
    }
}
