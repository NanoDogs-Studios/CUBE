using NanodogsToolkit.NanoVoice;
using Photon.Pun;
using System.Collections;
using UnityEngine;

/// <summary>
/// Handles voicelines for a survivor player.
/// </summary>
public class VoicelineHandler : MonoBehaviourPunCallbacks
{
    public BasePlayer player;
    private RoundManager roundManager;

    private SurvivorType survivor;
    private CubeCharacterData data;

    private void Start()
    {
        roundManager = FindFirstObjectByType<RoundManager>();
        if (roundManager != null)
        {
            roundManager.OnRoundStart += RoundStarted;
        }
    }

    private void RoundStarted()
    {
        survivor = player.GetEquippedSurvivor();
        data = survivor.character;

        if (player.GetPlayerType() == BasePlayer.PlayerType.Killer)
        {
            Debug.LogWarning("VoicelineHandler attached to a Killer. This should be on a Survivor.");
            this.enabled = false;
            return;
        }
    }

    private void Update()
    {
        if (player == null || survivor == null) return;

        if (player.GetPlayerType() != BasePlayer.PlayerType.Survivor) return;

        // Idle lines
        // first, get a random number between 10 and 70, this will be the time in seconds until the next idle line
        int time = UnityEngine.Random.Range(10, 70);

        var idleLines = System.Array.FindAll(
            data.voicelines,
            voiceLine => voiceLine.type == Voiceline.VoicelineType.Idle
        );

        if (idleLines.Length == 0) return;
        var selectedLine = idleLines[UnityEngine.Random.Range(0, idleLines.Length)];
        StartCoroutine(VoicelineCoroutine(selectedLine, time));
    }

    IEnumerator VoicelineCoroutine(Voiceline line, int waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        var lines = System.Array.FindAll(
            data.voicelines,
            voiceLine => voiceLine.type == line.type
        );
        var selectedLine = lines[UnityEngine.Random.Range(0, lines.Length)];

        AudioSource audioSource = player.GetComponent<AudioSource>();
        audioSource.clip = selectedLine.voiceline.voiceLine;
        Debug.Log($"Playing voiceline: {selectedLine.voiceline.lineID}");
        audioSource.Play();
    }
}