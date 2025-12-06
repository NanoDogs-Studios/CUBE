using UnityEngine;

/// <summary>
/// The base class for the cube characters. Tells the game most info about a character
/// </summary>
public class CubePlayerBase : ScriptableObject
{
    public Sprite image;
    public string _Name;
    public string Description;
    public string ScriptName;
    public int Cost;
    // out of 5
    [Range(0f, 5f)]
    public int Difficulty;
    public int Health;

    public float Speed;
    public int Stamina;

    public Ability[] abilities;

    public string _PlayFabItemID;

    public CubeCharacterData character;
}