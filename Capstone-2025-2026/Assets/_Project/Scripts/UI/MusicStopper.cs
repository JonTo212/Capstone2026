using FMODUnity;
using UnityEngine;

public class MusicStopper : MonoBehaviour
{
    public StudioEventEmitter musicEvent;

    private void OnTriggerEnter(Collider other)
    {
        //SHitty ass code im so sorry
        musicEvent.Stop();
    }
}
