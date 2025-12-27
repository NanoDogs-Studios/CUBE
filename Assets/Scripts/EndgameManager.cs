using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class EndgameManager : MonoBehaviour
{
    public RoundManager roundManager;

    public AudioClip thirtySeconds;
    public AudioClip tick;

    private AudioSource audioSource;

    GameObject volObj;
    Volume volume;

    float chromaticAberrationInitial;
    float lensDistortionInitial;
    float saturationInitial;

    private void Start()
    {
        volObj = GameObject.Find("GlobalVolume");
        if (volObj != null)
        {
            volume = volObj.GetComponent<Volume>();
        }

        audioSource = GetComponent<AudioSource>();
        roundManager.OnRoundStart += RoundStarted;
        roundManager.OnIntermissionStart += () =>
        {
            // Reset visual effects
            if (volume != null)
            {
                volume.profile.TryGet<ChromaticAberration>(out var chromaticAberration);
                if (chromaticAberration != null)
                {
                    chromaticAberration.intensity.value = chromaticAberrationInitial;
                }
                volume.profile.TryGet<LensDistortion>(out var lensDistortion);
                if (lensDistortion != null)
                {
                    lensDistortion.intensity.value = lensDistortionInitial;
                }
                volume.profile.TryGet<ColorAdjustments>(out var colorAdjustments);
                if (colorAdjustments != null)
                {
                    colorAdjustments.saturation.value = saturationInitial;
                }
            }
        };
    }

    private void RoundStarted()
    {
        InvokeRepeating(nameof(CheckTime), 1f, 1f);
    }

    private void CheckTime()
    {
        if (roundManager.currentTime == 30)
        {
            GetComponent<MusicManager>().roundSource.Stop();
            GetComponent<MusicManager>().roundSource.clip = thirtySeconds;
            GetComponent<MusicManager>().roundSource.Play();

            audioSource.Stop();
            audioSource.clip = thirtySeconds;
            audioSource.Play();

            if (volume != null)
            {
                volume.profile.TryGet<ChromaticAberration>(out var chromaticAberration);
                if (chromaticAberration != null)
                {
                    chromaticAberrationInitial = chromaticAberration.intensity.value;

                    float lerped = Mathf.Lerp(chromaticAberration.intensity.value, 0.5f, 3f);
                    chromaticAberration.intensity.value = lerped;
                }

                volume.profile.TryGet<LensDistortion>(out var lensDistortion);
                if (lensDistortion != null)
                {
                    lensDistortionInitial = lensDistortion.intensity.value;

                    float lerped1 = Mathf.Lerp(lensDistortion.intensity.value, 0.1f, 3f);
                    lensDistortion.intensity.value = lerped1;
                }

                volume.profile.TryGet<ColorAdjustments>(out var colorAdjustments);
                if (colorAdjustments != null)
                {
                    saturationInitial = colorAdjustments.saturation.value;

                    float lerped2 = Mathf.Lerp(colorAdjustments.saturation.value, 40f, 3f);
                    colorAdjustments.saturation.value = lerped2;
                }
            }
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
}
