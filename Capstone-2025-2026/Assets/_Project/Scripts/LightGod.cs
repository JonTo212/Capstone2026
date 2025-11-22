using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;

public class LightGod : MonoBehaviour
{

    //sound 
    private AudioSource audioSource;
    public AudioClip cleanseSound;

    //Components
    private AnimalJar animalJarScript;

    //particles
    public ParticleSystem explodeParticle;

    private void Start()
    {
        //get Components
        audioSource = GetComponent<AudioSource>();
        animalJarScript = GetComponent<AnimalJar>();

        //explodeParticle.Play();
    }

    public void Update()
    {
        
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("LightGodRay"))
        {
            //play particle effect
            explodeParticle.Play();

            // Play the clip as a one-shot
            audioSource.PlayOneShot(cleanseSound);
        }
    }


    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("LightGodRay"))
        {
            Clense();

            print("cleansing");
        }
    }


    private void Clense()
    {
        Invoke(nameof(Break), 3f);
    }


    private void Break()
    {
        //stop playing explode particle
        explodeParticle.Stop();



        animalJarScript.isBroken = true;

        //stop playing sound clip
        audioSource.Stop();
    }



}
