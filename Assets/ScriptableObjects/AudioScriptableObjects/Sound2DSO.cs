using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "Sound2DSO", menuName = "Scriptable Objects/Sound2DSO")]
public class Sound2DSO : ScriptableObject
{
    public string soundName;
    public AudioClip clip;
    public bool loop = false;
    public float volume = 1f;
    public AudioMixerGroup audioMixerGroup;
}
