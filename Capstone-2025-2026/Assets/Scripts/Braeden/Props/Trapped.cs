using UnityEngine;
using UnityEngine.Audio;

public class Trapped : MonoBehaviour
{
    public GameObject sparkleEffect;

    public AnimalJar animalJarScript;
    public float speed;


    //sound 
    private AudioSource audioSource;
    public AudioClip popSound;
    public AudioClip sparkleSound;
    public AudioClip fanFareSound;

    public bool audioPlayed = false;


    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        if (animalJarScript.isBroken == true)
        {
            SetFree();
        }
    }

    void SetFree()
    {
        //sound
        if (!audioSource.isPlaying && !audioPlayed)
        {
            audioSource.PlayOneShot(popSound,2);
            audioSource.PlayOneShot(sparkleSound,.2f);
            audioSource.PlayOneShot(fanFareSound,.3f);



            audioPlayed = true;
        }
   

        //set sparkle effect
        Instantiate(sparkleEffect, transform.position, Quaternion.identity);

        //fly away
        transform.position += new Vector3(speed, speed, 0) * Time.deltaTime;

        //destroy after 10 seconds
        Invoke(nameof(DestroyNPC), 10f);

    }

    void DestroyNPC()
    {
        Destroy(gameObject);   
    }
}
