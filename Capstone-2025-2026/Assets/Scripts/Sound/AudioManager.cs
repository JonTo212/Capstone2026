using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public bool playOnStart = true;
    [Header("------------Audio Source------------")]
    [SerializeField] private AudioSource musicSource1;
    [SerializeField] private AudioSource musicSource2;

    [SerializeField] private AudioSource sFXSource1;
    [SerializeField] private AudioSource sFXSource2;
    [SerializeField] private AudioSource sFXSource3;
    [SerializeField] private AudioSource sFXSource4;
    [SerializeField] private AudioSource sFXSource5;
    [SerializeField] private AudioSource sFXSource6;
    [SerializeField] private AudioSource sFXSource7;

    [SerializeField] private AudioSource ambienceSource1;

    public AudioSource MusicSource1 => musicSource1;
    public AudioSource MusicSource2 => musicSource2;

    public AudioSource SFXSource1 => sFXSource1;
    public AudioSource SFXSource2 => sFXSource2;
    public AudioSource SFXSource3 => sFXSource3;
    public AudioSource SFXSource4 => sFXSource4;
    public AudioSource SFXSource5 => sFXSource5;
    public AudioSource SFXSource6 => sFXSource6;
    public AudioSource SFXSource7 => sFXSource7;

    public AudioSource AmbienceSource1 => ambienceSource1;


    [Header("------------Music Clips------------")]
    [SerializeField] private AudioClip track1;
    [SerializeField] private AudioClip track2;
    [SerializeField] private AudioClip happyBirthday;
    public AudioClip Track1 => track1;
    public AudioClip Track2 => track2;
    public AudioClip HappyBirthday => happyBirthday;

    [Header("------------Ambience Clips------------")]
    [SerializeField] private AudioClip ambience1;
    public AudioClip Ambience1 => ambience1;

    [Header("------------Player Clips------------")]
    [SerializeField] private AudioClip playerHurt;
    [SerializeField] private AudioClip playerBadlyHurt;
    [SerializeField] private AudioClip playerSaved;
    [SerializeField] private AudioClip jump;
    [SerializeField] private AudioClip walk;
    [SerializeField] private AudioClip yank;
    [SerializeField] private AudioClip thrown;
    [SerializeField] private AudioClip tetherStart;
    [SerializeField] private AudioClip tetherEnd;
    [SerializeField] private AudioClip tetherTighten;
    //[SerializeField]  private AudioClip claspSolid;
    //[SerializeField]  private AudioClip claspSoft;
    public AudioClip PlayerHurt => playerHurt;
    public AudioClip PlayerBadlyHurt => playerBadlyHurt;
    public AudioClip PlayerSaved => playerSaved;
    public AudioClip Jump => jump;
    public AudioClip Walk => walk;
    public AudioClip Pull => yank;
    public AudioClip Thrown => thrown;
    public AudioClip TetherStart => tetherStart;
    public AudioClip TetherEnd => tetherEnd;
    public AudioClip TetherTighten => tetherTighten;
    //public AudioClip ClaspSolid => claspSolid;
    //public AudioClip ClaspSoft => claspSoft ;
    [Header("------------Enemy Clips------------")]
    [SerializeField] private AudioClip enemyAttack;
    [SerializeField] private AudioClip enemyPerish;
    public AudioClip EnemyAttack => enemyAttack;
    public AudioClip EnemyPerish => enemyPerish;

    [Header("------------Boss Clips------------")]
    [SerializeField] private AudioClip bossHurt;
    [SerializeField] private AudioClip bossPerish;
    [SerializeField] private AudioClip bossLaserCharge;
    [SerializeField] private AudioClip bossLaserFire;
    [SerializeField] private AudioClip bossSpawn;
    [SerializeField] private AudioClip bossProjectileBoom;
    public AudioClip BossHurt => bossHurt;
    public AudioClip BossPerish => bossPerish;
    public AudioClip BossLaserCharge => bossLaserCharge;
    public AudioClip BossLaserFire => bossLaserFire;
    public AudioClip BossSpawn => bossSpawn;
    public AudioClip BossProjectileBoom => bossProjectileBoom;

    

    [Header("------------Menu Clips------------")]
    [SerializeField] private AudioClip menuOk;
    [SerializeField] private AudioClip menuNo;
    public AudioClip MenuOk => menuOk;
    public AudioClip MenuNo => menuNo;



    [Header("------------Debugging Clips------------")]
    private AudioClip error;
    [SerializeField] private AudioClip empty;
    public AudioClip Empty => empty;



    void Start()
    {
        if (playOnStart)
        {
            PlayMusic(Track1, 1, 1);
        }

        PlayAmbience(ambience1, 1, 0.5f);
    }

    #region SFX Functions
    public void MenuOKFunc()
    {
        //Plays SFX
        SFXSource1.PlayOneShot(menuOk);

    }

    public void MenuNOFunc()
    {
        //Plays SFX
        SFXSource1.PlayOneShot(menuNo);

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
                case 5:
                    selectedSource = SFXSource5;
                    break;
                case 6:
                    selectedSource = SFXSource6;
                    break;
                case 7:
                    selectedSource = SFXSource7;
                    break;

            }

            //Adjust volume or defaults it otherwise
            selectedSource.volume = volume.Value;
            //Plays SFX
            selectedSource.clip = clip;
            selectedSource.Play();
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

    public void FadeMusic(AudioClip selectedTrack)
    {
        StartCoroutine(FadeTrackOut(selectedTrack));
    }

    public IEnumerator FadeTrackOut(AudioClip selectedTrack)
    {
        Debug.Log("Fading out");
        float timeToFade = 1.25f;
        float timeElapsed = 0;

        while (timeElapsed < timeToFade)
        {
            musicSource1.volume = Mathf.Lerp(1, 0, timeElapsed / timeToFade);
            yield return new WaitForSeconds(1.25f);
            PlayMusic(selectedTrack, 1);
            musicSource1.volume = Mathf.Lerp(0, 1, timeElapsed / timeToFade);
        }

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
