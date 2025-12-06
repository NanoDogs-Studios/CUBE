using System;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public AudioClip intermissionTrack;
    public AudioClip[] ambienceTracks;

    private AudioSource interSource;
    public AudioSource roundSource;
    public RoundManager roundManager;

    void Start()
    {
        interSource = GetComponent<AudioSource>();

        roundManager.OnIntermissionStart += PlayIntermissionMusic;
        roundManager.OnRoundStart += PlayAmbienceMusic;
    }

    private void PlayAmbienceMusic()
    {
        if (roundSource.isPlaying || interSource.isPlaying)
        { interSource.Stop(); roundSource.Stop(); }

        if (ambienceTracks.Length == 0) return;
        AudioClip selectedTrack = ambienceTracks[UnityEngine.Random.Range(0, ambienceTracks.Length)];
        roundSource.clip = selectedTrack;
        roundSource.Play();
    }

    private void PlayIntermissionMusic()
    {
        if (interSource.isPlaying)
        { interSource.Stop(); }

        interSource.clip = intermissionTrack;
        interSource.Play();
    }
}
