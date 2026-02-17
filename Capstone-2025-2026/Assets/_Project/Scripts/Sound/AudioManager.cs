using DG.Tweening;
using FMODUnity;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public bool playOnStart = true;
    

    private void Awake()
    {
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