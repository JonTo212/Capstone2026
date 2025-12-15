using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;
using Unity.Cinemachine;

public enum WindowMode
{
    fullScreen,
    windowed
}

public class SettingsMenu : MonoBehaviour
{
    [SerializeField] private AudioMixer myMixer;
    [SerializeField] private Slider masterSlider;

    [SerializeField] private Slider soundSlider;

    [SerializeField] private Slider ambienceSlider;
    
    [SerializeField] private Slider musicSlider;

    [SerializeField] private CinemachineInputAxisController inputAxisController;
    [SerializeField] private Slider xSensitivitySlider;
    [SerializeField] private Slider ySensitivitySlider;
    [SerializeField] private SpecialCameraController specialCameraController;

    [SerializeField] private TextMeshProUGUI windowModeText;
    private WindowMode windowMode = WindowMode.fullScreen;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetMasterVolume();
        SetSoundVolume();
        SetAmbienceVolume();
        SetMusicVolume();
        SetXSensitvity();
        SetYSensitivity();
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

    public void SetXSensitvity()
    {
        specialCameraController.playerXSens = xSensitivitySlider.value * 2;
    }

    public void SetYSensitivity()
    {
        specialCameraController.playerYSens = ySensitivitySlider.value * -1;
    }

    public void WindowModeButton()
    {
        if(windowMode == WindowMode.fullScreen)
        {
            windowMode = WindowMode.windowed;
            windowModeText.SetText("Windowed");
            Screen.fullScreenMode = FullScreenMode.Windowed;
            return;
        }
        if(windowMode == WindowMode.windowed)
        {
            windowMode = WindowMode.fullScreen;
            windowModeText.SetText("Fullscreen");
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            return;
        }
    }
}
