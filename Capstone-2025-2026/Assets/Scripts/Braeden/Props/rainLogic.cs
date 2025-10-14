using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

public class rainLogic : MonoBehaviour
{
    private ParticleSystem rainParticles;
    public Vector3 rainDirection;

    private AudioSource audioSource;
    public AudioClip rainSound;

    private float timerMax = 1.5f;
    public float currentTime;
    public bool inTheRain = false;


    //How much rain
    public enum RainIntensity
    {
        Low = 200,
        Medium = 1000,
        High = 5000,
    }
    public RainIntensity rainIntensity = RainIntensity.Medium;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rainParticles = GetComponent<ParticleSystem>();
        audioSource = GetComponent<AudioSource>();

        currentTime = timerMax;
    }

    // Update is called once per frame
    void Update()
    {
        ApplyRainIntensity();
        RainCountdown(); // is the player in the rain?
        RainAudio();
        RainDirection();
        CounterRotation(); // rotate in the opposite direction of the player so the direction of rain stays consistent
    }

    void ApplyRainIntensity()
    {
        var emission = rainParticles.emission;

        //set rainIntensity value (could be low med or high)
        emission.rateOverTime = (float)rainIntensity;

        //set audio level
        if (rainIntensity == RainIntensity.Low)
        {
            audioSource.volume = 0.4f;
        }
        else if (rainIntensity == RainIntensity.Medium)
        {
            audioSource.volume = 0.8f;
        }
        else if (rainIntensity == RainIntensity.High)
        {
            audioSource.volume = 1.2f;
        }
    }
    private void OnParticleCollision(GameObject other)
    {
        if (other.CompareTag("Player"))
        {
            inTheRain = true;
            currentTime = timerMax;
            //Debug.Log("Player in rain");
        }
    }

    void RainCountdown()
    {
        currentTime -= Time.deltaTime;

        if (currentTime <= 0f)
        {
            inTheRain = false;
            //Debug.Log("Player not in rain");
        }
    }

    void RainAudio()
    {
        //play rainsound
        if (!audioSource.isPlaying)
        {
            audioSource.clip = rainSound;
            audioSource.Play();
        }   

        //adjust pitch
        if (inTheRain)
        {
            if (audioSource.pitch < 1f)
            {
                audioSource.pitch += Time.deltaTime;
            }
        }
        else
        {
            if (audioSource.pitch > .4f)
            {
                audioSource.pitch -= Time.deltaTime;
            }
        }
        
    }

    void RainDirection()
    {
        var velocity = rainParticles.velocityOverLifetime;

        velocity.x = new ParticleSystem.MinMaxCurve(rainDirection.x);
        velocity.z = new ParticleSystem.MinMaxCurve(rainDirection.z);
    }

    void CounterRotation()
    {
        transform.rotation = Quaternion.Euler (0.0f, 0.0f, transform.parent.rotation.z * -1.0f);
    }

}
