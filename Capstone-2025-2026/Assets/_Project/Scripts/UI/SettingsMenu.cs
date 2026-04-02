using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;
using Unity.Cinemachine;
using System.Collections;

public enum WindowMode
{
    fullScreen,
    windowed
}

public class SettingsMenu : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioMixer myMixer;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider soundSlider;
    [SerializeField] private Slider ambienceSlider;
    [SerializeField] private Slider musicSlider;

    [Header("Sensitivity")]
    [SerializeField] private Slider xSensitivitySlider;
    [SerializeField] private Slider ySensitivitySlider;

    [Header("Screen Scale")]
    [SerializeField] private TextMeshProUGUI windowModeText;
    private WindowMode windowMode = WindowMode.fullScreen;

    public FMOD.Studio.Bus Master;
    public FMOD.Studio.Bus Music;
    public FMOD.Studio.Bus SFX;
    public FMOD.Studio.Bus Ambience;
    public float MasterVolume = 0.5f;
    public float SFXVolume = 0.5f;
    public float MusicVolume = 0.5f;
    public float AmbienceVolume = 0.5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Master = FMODUnity.RuntimeManager.GetBus("bus:/Master");
        Music = FMODUnity.RuntimeManager.GetBus("bus:/Master/Music");
        SFX = FMODUnity.RuntimeManager.GetBus("bus:/Master/SFX");
        Ambience = FMODUnity.RuntimeManager.GetBus("bus:/Master/Ambience");

        StartCoroutine(InitializeSettings());
    }

    private IEnumerator InitializeSettings()
    {
        yield return null;
        SetXSensitivity();
        SetYSensitivity();
        SetMasterVolume();
        SetSoundVolume();
        SetAmbienceVolume();
        SetMusicVolume();
    }

    public void SetMasterVolume()
    {
        MasterVolume = masterSlider.value;
        Master.setVolume(MasterVolume);
    }

    public void SetSoundVolume()
    {
        SFXVolume = soundSlider.value;
        SFX.setVolume(SFXVolume);
    }

    public void SetAmbienceVolume()
    {
        AmbienceVolume = ambienceSlider.value;
        Ambience.setVolume(AmbienceVolume);
    }

    public void SetMusicVolume()
    {
        MusicVolume = musicSlider.value;
        Music.setVolume(MusicVolume);
    }

    public void SetXSensitivity()
    {
        CameraRefData.Instance.CameraModeController.SetBaseXSensitivity(xSensitivitySlider.value / 100f);
    }

    public void SetYSensitivity()
    {
        CameraRefData.Instance.CameraModeController.SetBaseYSensitivity(ySensitivitySlider.value / 100f);
    }

    public void WindowModeButton()
    {
        if (windowMode == WindowMode.fullScreen)
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
