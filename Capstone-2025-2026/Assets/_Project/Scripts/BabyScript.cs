using NodeCanvas.Tasks.Actions;
using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;

public class BabyScript : MonoBehaviour
{
    public float timeInLight = 0f;
    public float timetoSave = 3f;
    public float returnSpeed = 0.1f;
    private Vector3 velocity; // need this for smoothdamp   


    //particles
    public ParticleSystem explodeParticle;

    public AudioManager audioManager;

    private bool saveStart = false;
    private bool coroutineStarted = false;

    //components
    private Rigidbody rb;
    private Collider col;
    public GameObject target;
    private Prop tetherScript;

    //materials
    public Material cleanMat;
    public Material corruptMat;

    public event Action OnEnterBag;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //change material to clean
        GetComponent<Renderer>().material = corruptMat;
        audioManager = GameObject.Find("AudioManager").GetComponent<AudioManager>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        tetherScript = GetComponent<Prop>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("LightGodRay"))
        {
            //countdown
            timeInLight += Time.deltaTime;

            //play particle effect
            explodeParticle.Play();

            //playsound 
            if (!saveStart)
            {
                audioManager.PlaySFX(audioManager.LightClense, 1, 1);
                saveStart = true;
            }

            //start save process
            if ((timeInLight >= timetoSave) && (!coroutineStarted))
            {
                StartCoroutine(Cleansed());
                coroutineStarted = true;
            }
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("LightGodRay"))
        {
            //reset countdown
            timeInLight = 0f;

            //stop particle effect
            explodeParticle.Stop();

            //stopsound
            //ASK JUAN TO ADD STOP SOUND FUNCTIONALITY
            //saveStart = false;
        }
    }


    IEnumerator Cleansed()
    {
        var returnBuffer = 0.5f;

        //disable components
        col.enabled = false;    
        rb.isKinematic = true;
        tetherScript.enabled = false;

        //fanfare sound
        audioManager.PlaySFX(audioManager.FanFare, 5, 1);

        //change material to clean
        GetComponent<Renderer>().material = cleanMat;

        //stop cleansing particle
        explodeParticle.Stop();

        //Pose
        var posePosition = transform.position + new Vector3(5f, 5f, 0);
        while (Vector3.Distance(transform.position, posePosition) > returnBuffer)
        {
            //face player
            transform.LookAt(target.transform.position);

            //transform.position = Vector3.MoveTowards(transform.position, target.transform.position, returnSpeed);
            transform.position = Vector3.Lerp(transform.position, posePosition, returnSpeed * .1f);

            yield return null;
        }

        yield return new WaitForSeconds(2f);


        //go to player backpack
        while (Vector3.Distance(transform.position, target.transform.position) > returnBuffer)
        {
            //transform.position = Vector3.MoveTowards(transform.position, target.transform.position, returnSpeed);
            transform.position = Vector3.Lerp(transform.position, target.transform.position, returnSpeed * .1f);
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, returnSpeed*.1f);

            yield return null;
        }

        InBag();
    }

    public void InBag()
    {
        //collect sound
        audioManager.PlaySFX(audioManager.Collection, 5, 1);
        OnEnterBag.Invoke();
        Destroy(gameObject);
    }
}
