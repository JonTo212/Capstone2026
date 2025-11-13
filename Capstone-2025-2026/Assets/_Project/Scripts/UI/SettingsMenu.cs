using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField] private AudioMixer myMixer;
    [SerializeField] private Slider masterSlider;

    [SerializeField] private Slider soundSlider;

    [SerializeField] private Slider ambienceSlider;
    
    [SerializeField] private Slider musicSlider;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        SetMasterVolume();
        SetSoundVolume();
        SetAmbienceVolume();
        SetMusicVolume();
    }

    public void SetMasterVolume()
    {
        float volume = masterSlider.value;
        myMixer.SetFloat("Master", Mathf.Log10(volume)*20);
    }

    public void SetSoundVolume()
    {
        float volume = soundSlider.value;
        myMixer.SetFloat("Sound", Mathf.Log10(volume)*20);
    }

    public void SetAmbienceVolume()
    {
        float volume = ambienceSlider.value;
        myMixer.SetFloat("Ambience", Mathf.Log10(volume)*20);
    }

    public void SetMusicVolume()
    {
        float volume = musicSlider.value;
        myMixer.SetFloat("Music", Mathf.Log10(volume)*20);
    }
    

}
