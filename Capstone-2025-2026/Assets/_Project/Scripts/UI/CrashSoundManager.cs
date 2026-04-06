using UnityEngine;
using FMODUnity;

public class CrashSoundManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public StudioEventEmitter first;
    public StudioEventEmitter second;
    public StudioEventEmitter third;
    public bool allPlayed= false ;


    public void FirstEmitter()
    {
        if(!allPlayed)first.Play();
    }

    public void SecondEmitter()
    {
        if (!allPlayed) second.Play();
    }
    public void ThirdEmitter()
    {
        if (!allPlayed) third.Play();
        allPlayed = true;
    }




}
