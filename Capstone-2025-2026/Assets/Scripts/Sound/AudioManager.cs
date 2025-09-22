using UnityEngine;

public class AudioManager : MonoBehaviour
{

    [Header("------------Audio Source------------")]
    [SerializeField] AudioSource musicSource1;
    [SerializeField] AudioSource musicSource2;

    [SerializeField] AudioSource SFXSource1;
    [SerializeField] AudioSource SFXSource2;
    [SerializeField] AudioSource SFXSource3;
    [SerializeField] AudioSource SFXSource4;

    [SerializeField] AudioSource Ambience1;


    [Header("------------Music Clips------------")]

    public AudioClip track1;

    [Header("------------Ambience Clips------------")]

    public AudioClip ambience1;

    [Header("------------SFX Clips------------")]
    public AudioClip jump;
    public AudioClip claspSolid;
    public AudioClip claspSoft;
    public AudioClip pull;
    public AudioClip thrown;
    public AudioClip dog;

    [Header("------------Debugging Clips------------")]
    public AudioClip error;
    public AudioClip empty;


    void Start()
    {
        musicSource1.clip = track1;
        musicSource1.Play();
    }

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

    // Update is called once per frame
    void Update()
    {
        
    }
}
