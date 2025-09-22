using UnityEngine;

public class AudioManager : MonoBehaviour
{

    [Header("------------Audio Source------------")]
    [SerializeField] private AudioSource musicSource1;
    [SerializeField] private AudioSource musicSource2;

    [SerializeField] private AudioSource SFXSource1;
    [SerializeField] private AudioSource SFXSource2;
    [SerializeField] private AudioSource SFXSource3;
    [SerializeField] private AudioSource SFXSource4;

    [SerializeField] private AudioSource AmbienceSource1;


    [Header("------------Music Clips------------")]
    [SerializeField] private AudioClip track1;
    public AudioClip Track1 => track1;

    [Header("------------Ambience Clips------------")]
    [SerializeField] private AudioClip ambience1;
    public AudioClip Ambience1 => ambience1;

    [Header("------------SFX Clips------------")]
    [SerializeField]  private AudioClip jump;
    [SerializeField]  private AudioClip claspSolid;
    [SerializeField]  private AudioClip claspSoft;
    [SerializeField]  private AudioClip pull;
    [SerializeField]  private AudioClip thrown;
    [SerializeField]  private AudioClip dog;


    public AudioClip Jump => jump;
    public AudioClip ClaspSolid => claspSolid;
    public AudioClip ClaspSoft => claspSoft ;
    public AudioClip Pull => pull;
    public AudioClip Thrown => thrown;
    public AudioClip Dog => dog;

    [Header("------------Debugging Clips------------")]
    private AudioClip error;
    private AudioClip empty;


    void Start()
    {
        musicSource1.clip = track1;
        musicSource1.Play();
    }

    #region SFX Functions
    //Choose CLIP, Choose CHANNEL. It'll play one loop of the clip at full volume. If the clip isn't filled out properly, error noise will play
    public void PlaySFX(AudioClip clip, int channel, float? volume = null)
    {
        if (clip != null)
        {
            //Sets audio Channel
            AudioSource selectedSource = null;
            switch (channel)
            {
                case 1:
                    selectedSource = SFXSource1;
                    break;
                case 2:
                    selectedSource = SFXSource2;
                    break;
                case 3:
                    selectedSource = SFXSource3;
                    break;
                case 4:
                    selectedSource = SFXSource4;
                    break;

            }

            //Adjust volume or defaults it otherwise
            selectedSource.volume = volume.Value;
            //Plays SFX
            selectedSource.PlayOneShot(clip);
        }
        //Missing Audio Error
        else
        {
            SFXSource1.volume = 0.2f;
            SFXSource1.PlayOneShot(error);
        }
    }

    //Choose CLIP, Choose CHANNEL, Choose RANGE. It'll play one loop of the clip at full volume with pitch varied between given range. If the clip isn't filled out properly, error noise will play
    public void PlaySFXVaried(AudioClip clip, int channel, float range, float? volume = 1)
    {
        if (clip != null)
        {
            //Sets audio Channel
            AudioSource selectedSource = null;
            switch (channel)
            {
                case 1:
                    selectedSource = SFXSource1;
                    break;
                case 2:
                    selectedSource = SFXSource2;
                    break;
                case 3:
                    selectedSource = SFXSource3;
                    break;
                case 4:
                    selectedSource = SFXSource4;
                    break;

            }

            //Adjust volume or defaults it otherwise
            selectedSource.volume = volume.Value;
            //Wiggles the pitch based on the range
            selectedSource.pitch = Random.Range(1 - range, 1 + range);
            //Plays SFX
            selectedSource.PlayOneShot(clip);
        }
        //Missing Audio Error
        else
        {
            SFXSource1.volume = 0.2f;
            SFXSource1.PlayOneShot(error);
        }
    }

    #endregion

    #region Music Functions

    public void PlayMusic(AudioClip track, int channel, float? volume = null)
    {
        if (track != null)
        {
            //Sets audio Channel
            AudioSource selectedSource = null;
            switch (channel)
            {
                case 1:
                    selectedSource = musicSource1;
                    break;
                case 2:
                    selectedSource = musicSource2;
                    break;
            }

            //Adjust volume or defaults it otherwise
            selectedSource.volume = volume.Value;
            //Plays SFX
            selectedSource.clip = track;
            selectedSource.Play();
        }
        //Missing Audio Error
        else
        {
            SFXSource1.volume = 0.2f;
            SFXSource1.PlayOneShot(error);
        }
    }

    public void StopMusic(int channel)
    {
        switch (channel)
        {
            case 1:
                musicSource1.Stop();
                break;
            case 2:
                musicSource2.Stop();
                break;
        }
    }

    public void PlayMusicSynced(AudioClip track1, AudioClip track2, float? volume = null)
    {
        if (track1 != null && track2 != null)
        {
         
            //Adjust volume or defaults it otherwise
            musicSource1.volume = volume.Value;
            musicSource2.volume = volume.Value;
            //Plays SFX
            musicSource1.clip = track1;
            musicSource2.clip = track2;
            musicSource1.Play();
            musicSource2.Play();
        }
        //Missing Audio Error
        else
        {
            SFXSource1.volume = 0.2f;
            SFXSource1.PlayOneShot(error);
        }
    }

    public void StopMusicSynced()
    {
        musicSource1.Stop();
        musicSource2.Stop();
    }

    #endregion

    #region Ambience Functions


    public void PlayAmbience(AudioClip track, int? channel = 1, float? volume = null)
    {
        if (track != null)
        {
            //Sets audio Channel
            AudioSource selectedSource = null;
            switch (channel)
            {
                case 1:
                    selectedSource = AmbienceSource1;
                    break;
            }

            //Adjust volume or defaults it otherwise
            selectedSource.volume = volume.Value;
            //Plays SFX
            selectedSource.clip = track;
            selectedSource.Play();
        }
        //Missing Audio Error
        else
        {
            SFXSource1.volume = 0.2f;
            SFXSource1.PlayOneShot(error);
        }
    }

    public void StopAmbience(int channel)
    {
        switch (channel)
        {
            case 1:
                musicSource1.Stop();
                break;
            case 2:
                musicSource2.Stop();
                break;
        }
    }
    #endregion
}
