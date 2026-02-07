using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class VolumeSettings : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    void Start()
    {
        if (PlayerPrefs.HasKey("Master Volume") && PlayerPrefs.HasKey("Music Volume") && PlayerPrefs.HasKey("SFX Volume"))
        {
            LoadVolume();
        }
        else
        {
        SetMasterVolume();
        SetMusicVolume();
        SetSFXVolume();
        }
    }


    public void SetMasterVolume()
    {
        float volume = masterSlider.value;
        audioMixer.SetFloat("Master Volume", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("Master Volume", volume);
    }
    public void SetMusicVolume()
    {
        float volume = musicSlider.value;
        audioMixer.SetFloat("Music Volume", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("Music Volume", volume);
    }
    public void SetSFXVolume()
    {
        float volume = sfxSlider.value;
        audioMixer.SetFloat("SFX Volume", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("SFX Volume", volume);
    }

    private void LoadVolume ()
    {
        masterSlider.value = PlayerPrefs.GetFloat("Master Volume");
        musicSlider.value = PlayerPrefs.GetFloat("Music Volume");
        sfxSlider.value = PlayerPrefs.GetFloat("SFX Volume");

        SetMasterVolume();
        SetMusicVolume();
        SetSFXVolume();
    }
}
