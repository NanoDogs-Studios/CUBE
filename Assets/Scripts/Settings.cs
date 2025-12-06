using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    [Header("Audio")]
    public AudioMixer mixer;
    public Slider master;
    public Slider sfx;
    public Slider music;

    [Header("Graphics UI")]
    public TMP_Dropdown vsyncDropdown;
    public TMP_Dropdown graphicsDropdown;

    // PlayerPrefs keys (consistent)
    private const string KEY_FULLSCREEN = "Fullscreen";
    private const string KEY_MASTER = "MasterVolume";
    private const string KEY_SFX = "SFXVolume";
    private const string KEY_MUSIC = "MusicVolume";
    private const string KEY_VSYNC = "Vsync";
    private const string KEY_QUALITY = "Quality";

    private void Start()
    {
        LoadSettings();
    }

    public void SetFullscreen(bool fullscreen)
    {
        Screen.fullScreen = fullscreen;
        PlayerPrefs.SetInt(KEY_FULLSCREEN, fullscreen ? 1 : 0);
    }

    public void SetMasterVolume(float volume)
    {
        if (mixer != null)
            mixer.SetFloat("masterVolume", volume);
        PlayerPrefs.SetFloat(KEY_MASTER, volume);
    }

    public void SetSFXVolume(float volume)
    {
        if (mixer != null)
            mixer.SetFloat("sfxVolume", volume);
        PlayerPrefs.SetFloat(KEY_SFX, volume);
    }

    public void SetMusicVolume(float volume)
    {
        if (mixer != null)
            mixer.SetFloat("musicVolume", volume);
        PlayerPrefs.SetFloat(KEY_MUSIC, volume);
    }

    public void SetVsync(int vsyncCount)
    {
        QualitySettings.vSyncCount = vsyncCount;
        PlayerPrefs.SetInt(KEY_VSYNC, vsyncCount);
    }

    public void SetQuality(int quality)
    {
        QualitySettings.SetQualityLevel(quality);
        PlayerPrefs.SetInt(KEY_QUALITY, quality);
    }

    public void SaveSettings()
    {
        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        // Master volume
        if (PlayerPrefs.HasKey(KEY_MASTER))
        {
            float masterVol = PlayerPrefs.GetFloat(KEY_MASTER);
            if (mixer != null) mixer.SetFloat("masterVolume", masterVol);
            if (master != null) master.value = masterVol;
        }

        // SFX volume
        if (PlayerPrefs.HasKey(KEY_SFX))
        {
            float sfxVol = PlayerPrefs.GetFloat(KEY_SFX);
            if (mixer != null) mixer.SetFloat("sfxVolume", sfxVol);
            if (sfx != null) sfx.value = sfxVol;
        }

        // Music volume
        if (PlayerPrefs.HasKey(KEY_MUSIC))
        {
            float musicVol = PlayerPrefs.GetFloat(KEY_MUSIC);
            if (mixer != null) mixer.SetFloat("musicVolume", musicVol);
            if (music != null) music.value = musicVol;
        }

        // Quality level
        if (PlayerPrefs.HasKey(KEY_QUALITY))
        {
            int quality = PlayerPrefs.GetInt(KEY_QUALITY);
            QualitySettings.SetQualityLevel(quality);
            if (graphicsDropdown != null)
            {
                graphicsDropdown.value = quality;
                graphicsDropdown.RefreshShownValue();
            }
        }

        // Fullscreen
        if (PlayerPrefs.HasKey(KEY_FULLSCREEN))
        {
            Screen.fullScreen = PlayerPrefs.GetInt(KEY_FULLSCREEN) != 0;
        }

        // VSync (independent of fullscreen)
        if (PlayerPrefs.HasKey(KEY_VSYNC))
        {
            int vsync = PlayerPrefs.GetInt(KEY_VSYNC);
            QualitySettings.vSyncCount = vsync;
            if (vsyncDropdown != null)
            {
                vsyncDropdown.value = vsync;
                vsyncDropdown.RefreshShownValue();
            }
        }
    }
}
