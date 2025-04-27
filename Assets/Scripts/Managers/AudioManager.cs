using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Collections;
using UnityEngine.Audio;
using System;
using System.Linq;

public class AudioManager : PersistentNetworkSingleton<AudioManager>
{
    [Header("2D Sounds")]
    [SerializeField] private AudioSource global2DAudioSource;
    [SerializeField] private List<Sound2DSO> sounds2DList = new List<Sound2DSO>();

    [Header("3D Sounds")]
    private Dictionary<Sound3DSO, Queue<AudioSource>> soundPools = new Dictionary<Sound3DSO, Queue<AudioSource>>();
    [SerializeField] private GameObject audioSourceNetworkPrefab;
    [SerializeField] private GameObject audioSourceLocalPrefab;
    [SerializeField] private Sounds3DSOList sounds3DListSO;
    [Header("Mixer References")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioMixerSnapshot defaultSnapshot;
    [SerializeField] private AudioMixerSnapshot muffledSnapshot;

    private void Start()
    {
        global2DAudioSource = GetComponent<AudioSource>();
    }
    private AudioClip Get2DSound(string soundName)
    {
        return sounds2DList.FirstOrDefault(s => s.name == soundName).clip;
    }
    public void Play2DSound(string soundName, float volume, AudioMixerGroup audioMixerGroup, bool isLooping, bool isOneShot)
    {
        AudioClip clip = Get2DSound(soundName);
        if (clip == null) return;

        global2DAudioSource.clip = clip;
        global2DAudioSource.volume = volume;
        global2DAudioSource.loop = isLooping;
        global2DAudioSource.outputAudioMixerGroup = audioMixerGroup;
        if (isOneShot && !isLooping)
        {
            global2DAudioSource.PlayOneShot(clip, volume);
        }
        else
        {
            global2DAudioSource.Play();
        }

    }
    public void Play3DSound(Sound3DSO soundSO, Vector3 position, bool isOneShot, float? volume = null, float? minDistance = null, float? maxDistance = null)
    {
        AudioSource audioSource = Get3DSource(soundSO);
        audioSource.transform.position = position;
        audioSource.gameObject.SetActive(true);

        audioSource.volume = volume ?? soundSO.volume;
        audioSource.minDistance = minDistance ?? soundSO.minDistance;
        audioSource.maxDistance = maxDistance ?? soundSO.maxDistance;

        if (!soundSO.loop && isOneShot)
        {
            audioSource.PlayOneShot(soundSO.clip, soundSO.clip.length);
            StartCoroutine(ReturnAfterDuration(audioSource, soundSO.clip.length, soundSO));
        }
        else
        {
            audioSource.Play();
        }

    }
    [Rpc(SendTo.Server)]
    public void Play3DSoundServerRpc(int index, Vector3 position, bool isOneShot, float volume, float minDistance, float maxDistance)
    {
        Play3DSound(sounds3DListSO.sound3DSOList[index], position, isOneShot, volume, minDistance, maxDistance);
    }

    private AudioSource CreateNew3DAudioPool(Sound3DSO soundSO)
    {
        GameObject soundObj = Instantiate(audioSourceNetworkPrefab, transform);
        if (IsServer)
        {
            NetworkObject soundNetObj = soundObj.GetComponent<NetworkObject>();
            if (soundNetObj != null)
            {
                soundNetObj.Spawn(true);
            }
        }
        AudioSource audioSource = soundObj.GetComponent<AudioSource>();
        if (audioSource == null) audioSource.AddComponent<AudioSource>();

        audioSource.clip = soundSO.clip;
        audioSource.loop = soundSO.loop;
        audioSource.minDistance = soundSO.minDistance;
        audioSource.maxDistance = soundSO.maxDistance;
        audioSource.volume = soundSO.volume;
        audioSource.outputAudioMixerGroup = soundSO.audioMixerGroup;

        soundObj.SetActive(false);
        return audioSource;
    }
    private AudioSource Get3DSource(Sound3DSO soundSO)
    {
        if (!soundPools.ContainsKey(soundSO))
            soundPools[soundSO] = new Queue<AudioSource>();

        var pool = soundPools[soundSO];

        if (pool.Count > 0)
            return pool.Dequeue();
        else
            return CreateNew3DAudioPool(soundSO);
    }

    private IEnumerator ReturnAfterDuration(AudioSource audioSource, float duration, Sound3DSO soundSO)
    {
        yield return new WaitForSeconds(duration);
        audioSource.Stop();
        audioSource.gameObject.SetActive(false);
        soundPools[soundSO].Enqueue(audioSource);
    }
    public void Stop3DSound(AudioSource audioSource, Sound3DSO soundSO)
    {
        if (audioSource == null) return;
        audioSource.Stop();
        audioSource.gameObject.SetActive(false);
        soundPools[soundSO].Enqueue(audioSource);
    }
    public void TransitionToSnapshot(AudioMixerSnapshot snapshot, float timeToBlend = 0.5f)
    {
        snapshot.TransitionTo(timeToBlend);
    }

    public void SetNormalAudio()
    {
        TransitionToSnapshot(defaultSnapshot);
    }

    public void SetMuffledAudio()
    {
        TransitionToSnapshot(muffledSnapshot);
    }

    private int Get3DSoundFromList(Sound3DSO sound3DSO)
    {
        return sounds3DListSO.sound3DSOList.IndexOf(sound3DSO);
    }
}
