using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Plays end-of-round audio + smoothly ramps post-processing when time hits 30 seconds.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class EndgameManager : MonoBehaviour
{
    public RoundManager roundManager;

    [Header("Audio")]
    public AudioClip thirtySeconds;
    public AudioClip tick;

    [Header("Post Processing")]
    [Tooltip("Name of the GameObject containing your global Volume component.")]
    public string globalVolumeName = "Global Volume";

    [Tooltip("How long (seconds) the effects take to ramp in.")]
    public float effectRampDuration = 1.25f;

    [Tooltip("Target values when 30 seconds remaining.")]
    public float targetChromatic = 0.5f;
    public float targetLensDistortion = 0.1f;
    public float targetSaturation = 40f;

    private AudioSource audioSource;

    private Volume volume;
    private ChromaticAberration chromatic;
    private LensDistortion lens;
    private ColorAdjustments colorAdj;

    // initial values (captured once at Start)
    private float chromaticInitial;
    private float lensInitial;
    private float saturationInitial;

    private Coroutine rampCoroutine;
    private bool rampTriggered;

    private void Start()
    {
        // Cache components
        audioSource = GetComponent<AudioSource>();

        var volObj = GameObject.Find(globalVolumeName);
        if (volObj) volume = volObj.GetComponent<Volume>();

        // Cache overrides + initial values once (don’t keep overwriting them at 30s)
        if (volume && volume.profile)
        {
            volume.profile.TryGet(out chromatic);
            volume.profile.TryGet(out lens);
            volume.profile.TryGet(out colorAdj);

            if (chromatic != null) chromaticInitial = chromatic.intensity.value;
            if (lens != null) lensInitial = lens.intensity.value;
            if (colorAdj != null) saturationInitial = colorAdj.saturation.value;
        }

        // Subscribe
        roundManager.OnRoundStart += RoundStarted;
        roundManager.OnIntermissionStart += OnIntermission;
    }

    private void OnDestroy()
    {
        // Unsubscribe (prevents leaks / double subscriptions when reloading scenes)
        if (roundManager != null)
        {
            roundManager.OnRoundStart -= RoundStarted;
            roundManager.OnIntermissionStart -= OnIntermission;
        }
    }

    private void RoundStarted()
    {
        rampTriggered = false;
        InvokeRepeating(nameof(CheckTime), 1f, 1f);
    }

    private void OnIntermission()
    {
        // Reset visuals instantly back to initial.
        ResetVisuals();

        // Stop any ramp coroutine.
        if (rampCoroutine != null)
        {
            StopCoroutine(rampCoroutine);
            rampCoroutine = null;
        }

        CancelInvoke(nameof(CheckTime));

        // Stop music
        var music = GetComponent<MusicManager>();
        if (music != null) music.roundSource.Stop();
    }

    private void CheckTime()
    {
        // If currentTime is a float, consider using <= 30 and a guard instead of == 30.
        if (!rampTriggered && roundManager.currentTime == 30)
        {
            rampTriggered = true;

            // music
            var music = GetComponent<MusicManager>();
            if (music != null)
            {
                music.roundSource.Stop();
                music.roundSource.clip = thirtySeconds;
                music.roundSource.Play();
            }

            // sfx
            audioSource.Stop();
            audioSource.clip = thirtySeconds;
            audioSource.Play();

            // start smooth ramp
            if (rampCoroutine != null) StopCoroutine(rampCoroutine);
            rampCoroutine = StartCoroutine(RampEffects(chromaticInitial, lensInitial, saturationInitial,
                                                       targetChromatic, targetLensDistortion, targetSaturation,
                                                       effectRampDuration));
        }
        else if (roundManager.currentTime <= 10 && roundManager.currentTime > 0)
        {
            audioSource.PlayOneShot(tick);
        }
        else if (roundManager.currentTime <= 0)
        {
            CancelInvoke(nameof(CheckTime));
        }
    }

    private System.Collections.IEnumerator RampEffects(float chromStart, float lensStart, float satStart,
                                                      float chromEnd, float lensEnd, float satEnd,
                                                      float duration)
    {
        if (duration <= 0f)
        {
            ApplyEffects(chromEnd, lensEnd, satEnd);
            yield break;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float eased = Mathf.SmoothStep(0f, 1f, t);

            float c = Mathf.Lerp(chromStart, chromEnd, eased);
            float l = Mathf.Lerp(lensStart, lensEnd, eased);
            float s = Mathf.Lerp(satStart, satEnd, eased);

            ApplyEffects(c, l, s);
            yield return null;
        }

        ApplyEffects(chromEnd, lensEnd, satEnd);
    }

    private void ApplyEffects(float chromValue, float lensValue, float saturationValue)
    {
        if (chromatic != null) chromatic.intensity.value = chromValue;
        if (lens != null) lens.intensity.value = lensValue;
        if (colorAdj != null) colorAdj.saturation.value = saturationValue;
    }

    private void ResetVisuals()
    {
        if (chromatic != null) chromatic.intensity.value = chromaticInitial;
        if (lens != null) lens.intensity.value = lensInitial;
        if (colorAdj != null) colorAdj.saturation.value = saturationInitial;
    }
}
