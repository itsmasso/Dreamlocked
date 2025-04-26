using System;
using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "Sound3DSO", menuName = "Scriptable Objects/Sound3DSO")]
public class Sound3DSO : ScriptableObject
{
    public string soundName;
    public AudioClip clip;
    public bool loop = false;
    public float minDistance = 1f;
    public float maxDistance = 30f;
    public float volume = 1f;
    public AudioMixerGroup audioMixerGroup;
}

