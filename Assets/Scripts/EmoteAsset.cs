using UnityEngine;
using System.Collections.Generic;

public class EmoteAsset : ScriptableObject
{
    [Tooltip("The display name of the emote.")]
    public string emoteName = "New Emote";

    [Tooltip("The icon image representing the emote.")]
    public Sprite image;

    [Tooltip("The cost of the emote in in-game currency.")]
    public int cost = 100;

    [Tooltip("The PlayFab Item ID associated with this emote.")]
    public string PlayFabItemId = "emote_newemote";

    [Tooltip("A brief description of the emote.")]
    [TextArea]
    public string Description = "A new emote.";

    [Tooltip("The prefab GameObjects representing the keyframe poses.")]
    public GameObject[] keyframes = new GameObject[1];

    [Tooltip("The total duration of the emote animation.")]
    public float emoteDuration = 1.0f;

    [Tooltip("Should the emote loop when played?")]
    public bool loopEmote = false;
}