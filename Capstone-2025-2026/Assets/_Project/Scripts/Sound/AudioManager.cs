using DG.Tweening;
using FMODUnity;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public bool playOnStart = true;

    public FMOD.Studio.Bus Master;
    public FMOD.Studio.Bus Music;
    public FMOD.Studio.Bus SFX;
    public FMOD.Studio.Bus Ambience;
    public float MasterVolume = 0.5f;
    public float SFXVolume = 0.5f;
    public float MusicVolume = 0.5f;
    public float AmbienceVolume = 0.5f;

    private void Awake()
    {
        Master = FMODUnity.RuntimeManager.GetBus("bus:/Master");
        Music = FMODUnity.RuntimeManager.GetBus("bus:/Master/Music");
        SFX = FMODUnity.RuntimeManager.GetBus("bus:/Master/SFX");
        Ambience = FMODUnity.RuntimeManager.GetBus("bus:/Master/Ambience");


        Master.setVolume(MasterVolume);
        SFX.setVolume(MasterVolume);
        Music.setVolume(MasterVolume);
        Ambience.setVolume(MasterVolume);

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    void Start()
    {
        if (playOnStart)
        {
            RuntimeManager.PlayOneShot("MenuOk", transform.position);
        }

        RuntimeManager.PlayOneShot("MenuOk", transform.position);
    }

    public void MenuOKFunc()
    {
        //Plays SFX
        RuntimeManager.PlayOneShot("event:/MenuSelect", transform.position);

    }

    public void MenuNOFunc()
    {
        //Plays SFX
        RuntimeManager.PlayOneShot("event:/MenuBack", transform.position);

    }
    
}