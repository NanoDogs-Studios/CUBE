using NanodogsToolkit.NanoVoice;
using UnityEngine;

/// <summary>
/// this class is for the PHYSICAL character (the in-round character)
/// </summary>
[CreateAssetMenu(fileName = "Character Data", menuName = "ScriptableObjects/Character Data")]
public class CubeCharacterData : ScriptableObject
{
    [Header("Voicelines")]
    public Voiceline[] voicelines;

    [Header("Chase")]
    public AudioClip layer1;
    public AudioClip layer2;
    public AudioClip layer3;
    public AudioClip layer4;

    [Header("Appearance")]
    public Accessory[] accessories;
    public Material bodyMaterial;
}

[System.Serializable]
public class Voiceline
{
    public NanoVoiceLine voiceline;
    public VoicelineType type;
    public enum VoicelineType
    {
        Idle,
        Hurt,
        Chase,
        Ability1,
        Ability2,
    }
}