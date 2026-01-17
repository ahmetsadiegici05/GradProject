using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private void Start()
    {
        LoadSettings();

        if (musicSlider != null)
        {
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
    }

    private void LoadSettings()
    {
        // Varsayılan değerler veya kaydedilmiş değerler
        float musicVol = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 1f);

        if (musicSlider != null) musicSlider.value = musicVol;
        if (sfxSlider != null) sfxSlider.value = sfxVol;

        // Başlangıçta uygula
        ApplyMusicVolume(musicVol);
        ApplySFXVolume(sfxVol);
    }

    public void OnMusicVolumeChanged(float value)
    {
        ApplyMusicVolume(value);
        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();
    }

    public void OnSFXVolumeChanged(float value)
    {
        ApplySFXVolume(value);
        PlayerPrefs.SetFloat("SFXVolume", value);
        PlayerPrefs.Save();
    }

    private void ApplyMusicVolume(float value)
    {
        if (BackgroundMusic.Instance != null)
        {
            BackgroundMusic.Instance.SetVolume(value);
        }
    }

    private void ApplySFXVolume(float value)
    {
        if (SoundManager.instance != null)
        {
            SoundManager.instance.SetVolume(value);
        }
    }
}
