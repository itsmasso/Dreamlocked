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
    [SerializeField] private GameObject audioSource2DPrefab;
    private List<AudioSource> active2DSounds = new List<AudioSource>();
    [SerializeField] private List<Sound2DSO> sounds2DList = new List<Sound2DSO>();

    [Header("3D Sounds")]
    private List<AudioSource> active3DSounds = new List<AudioSource>();
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

    public Sound2DSO Get2DSound(string soundName)
    {
        return sounds2DList.FirstOrDefault(s => s.name == soundName);
    }
    private IEnumerator FadeAudio(AudioSource source, float targetVolume, float duration, bool is3D = false, Sound3DSO related3DSoundSO = null)
    {
        if (source == null)
            yield break;

        float startVolume = source.volume;
        // Smart check
        if (targetVolume > startVolume)
        {
            // Fading IN
            source.volume = 0f;
            startVolume = 0f;
        }
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, timer / duration);
            yield return null;
        }

        source.volume = targetVolume;

        if (Mathf.Approximately(targetVolume, 0f))
        {
            source.Stop();

            if (is3D)
            {
                source.gameObject.SetActive(false);
                if (related3DSoundSO != null)
                {
                    soundPools[related3DSoundSO].Enqueue(source);
                }
                active3DSounds.Remove(source);
            }
            else
            {
                active2DSounds.Remove(source);
                Destroy(source.gameObject);
            }
        }
    }

    public void Play2DSound(Sound2DSO soundSO, float fadeInDuration, bool isOneShot = false)
    {
        if (soundSO == null || soundSO.clip == null)
            return;
        // Prevent duplicate if not one-shot
        if (!isOneShot)
        {
            var alreadyPlaying = active2DSounds.FirstOrDefault(s => s != null && s.clip == soundSO.clip && s.isPlaying);
            if (alreadyPlaying != null)
                return; // already playing, don't duplicate
        }
        GameObject new2DSoundObj = Instantiate(audioSource2DPrefab, transform);
        AudioSource source = new2DSoundObj.GetComponent<AudioSource>();

        source.clip = soundSO.clip;
        source.volume = soundSO.volume;
        source.loop = soundSO.loop;
        source.outputAudioMixerGroup = soundSO.audioMixerGroup;
        source.spatialBlend = 0f; // 2D sound
        if (isOneShot)
        {
            source.PlayOneShot(soundSO.clip, soundSO.volume);
        }
        else
        {
            source.Play();
        }

        active2DSounds.Add(source);
        StartCoroutine(FadeAudio(source, soundSO.volume, fadeInDuration));

    }
    public void Stop2DSound(Sound2DSO soundSO, float fadeOutDuration)
    {
        var source = active2DSounds.FirstOrDefault(s => s.clip == soundSO.clip);
        if (source != null)
        {
            StartCoroutine(FadeAudio(source, 0f, fadeOutDuration));
            // source.Stop();
            // active2DSounds.Remove(source);
            // Destroy(source.gameObject);
        }
    }

    public void Play3DSound(Sound3DSO soundSO, Vector3 position, Transform parent, bool isOneShot, float? volume = null, float? minDistance = null, float? maxDistance = null, bool preventDuplicates = true, float fadeInDuration = 0f)
    {
        if (!isOneShot && preventDuplicates)
        {
            // Prevent duplicates for looping 3D sounds
            var alreadyPlaying = active3DSounds.FirstOrDefault(s => s != null && s.clip == soundSO.clip && s.isPlaying);
            if (alreadyPlaying != null)
                return; // already playing, don't duplicate
        }
        AudioSource audioSource = Get3DSource(soundSO);
        audioSource.transform.position = position;
        if (parent != null)
            audioSource.transform.parent = parent;
        audioSource.gameObject.SetActive(true);

        float finalVolume = volume ?? soundSO.volume;
        audioSource.minDistance = minDistance ?? soundSO.minDistance;
        audioSource.maxDistance = maxDistance ?? soundSO.maxDistance;

        if (!soundSO.loop && isOneShot)
        {
            audioSource.PlayOneShot(soundSO.clip, finalVolume);
            StartCoroutine(ReturnAfterDuration(audioSource, soundSO.clip.length, soundSO));
        }
        else
        {
            if (fadeInDuration > 0f)
            {
                audioSource.volume = 0f;
                audioSource.Play();
                StartCoroutine(FadeAudio(audioSource, finalVolume, fadeInDuration, is3D: true, related3DSoundSO: soundSO));
            }
            else
            {
                audioSource.volume = finalVolume;
                audioSource.Play();
            }
        }
        active3DSounds.Add(audioSource);
    }
    [Rpc(SendTo.Server)]
    public void Play3DSoundServerRpc(int index, Vector3 position, bool isOneShot, float volume, float minDistance, float maxDistance, bool preventDupes, NetworkObjectReference networkObjectReference, float fadeInDuration = 0f)
    {
        if (index < 0 || index >= sounds3DListSO.sound3DSOList.Count)
        {
            Debug.LogError($"[AudioManager] Invalid 3D Sound Index: {index}. List Count: {sounds3DListSO.sound3DSOList.Count}");
            return;
        }
        if (networkObjectReference.TryGet(out NetworkObject networkObject))
        {
            Play3DSound(sounds3DListSO.sound3DSOList[index], position, networkObject.transform, isOneShot, volume, minDistance, maxDistance, preventDupes, fadeInDuration);
        }
        else
        {
            Play3DSound(sounds3DListSO.sound3DSOList[index], position, null, isOneShot, volume, minDistance, maxDistance, preventDupes, fadeInDuration);
        }

    }

    public void Play3DServerSoundDelayed(float delay, int index, Vector3 position, bool isOneShot, float volume, float minDistance, float maxDistance, bool preventDupes, NetworkObjectReference networkObjectReference, float fadeInDuration = 0f)
    {
        StartCoroutine(Start3DSoundDelayed(delay, index, position, isOneShot, volume, minDistance, maxDistance, preventDupes, networkObjectReference, fadeInDuration));
    }
    private IEnumerator Start3DSoundDelayed(float delay, int index, Vector3 position, bool isOneShot, float volume, float minDistance, float maxDistance, bool preventDupes, NetworkObjectReference networkObjectReference, float fadeInDuration = 0f)
    {
        yield return new WaitForSeconds(delay);
        Play3DSoundServerRpc(index, position, isOneShot, volume, minDistance, maxDistance, preventDupes, networkObjectReference, fadeInDuration);
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
    [Rpc(SendTo.Server)]
    public void Stop3DSoundServerRpc(int index, float fadeInDuration = 0f)
    {
        Sound3DSO sound3DSO = sounds3DListSO.sound3DSOList[index];
        Stop3DSound(sound3DSO, fadeInDuration);
    }
    public void Stop3DSound(Sound3DSO soundSO, float fadeOutDuration = 0.5f)
    {
        var source = active3DSounds.FirstOrDefault(s => s != null && s.clip == soundSO.clip && s.isPlaying);
        if (source != null)
        {
            StartCoroutine(FadeAudio(source, 0f, fadeOutDuration, is3D: true, related3DSoundSO: soundSO));
        }
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

    public int Get3DSoundFromList(Sound3DSO sound3DSO)
    {
        return sounds3DListSO.sound3DSOList.IndexOf(sound3DSO);
    }
    public void ClearAllAudio()
    {
        // Clear 2D Sounds
        if (IsServer)
        {
            foreach (var soundSource in active2DSounds)
            {
                if (soundSource != null)
                {
                    soundSource.Stop();
                    Destroy(soundSource.gameObject);
                }
            }
            active2DSounds.Clear();
        }

        // Clear 3D Sounds — only the server/host may actually despawn NetworkObjects
        if (IsServer) 
        {
            foreach (var soundSource in active3DSounds)
            {
                if (soundSource != null)
                {
                    soundSource.Stop();
                    soundSource.gameObject.SetActive(false);

                    var netObj = soundSource.GetComponent<NetworkObject>();
                    if (netObj != null)
                    {
                        netObj.Despawn();
                    }
                    else
                    {
                        // fallback for any purely-local AudioSources
                        Destroy(soundSource.gameObject);
                    }
                }
            }
            active3DSounds.Clear();
        }

        Debug.Log("AudioManager: Cleared all active sounds.");
    }
}
