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

    //respawning
    private Vector3 spawnPosition;
    public bool isFalling = false;
    private ParticleSystem tinyTornado;

    public float returnCameraViewBuffer = 0.1f;
    public float returnBuffer = 0.1f;
    public float respawnHeight = 5f;
    Camera camera;
    private Ray cameraCenterRay;
    public float distanceFromCameraView = 20f;
    public float spawnLimit = 50f;
    public float playerRangeLimit = 50f;
    public Transform playerLocation;

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


        //set spawn position
        spawnPosition = transform.position;
        camera = Camera.main;


        tinyTornado = GetComponentInChildren<ParticleSystem>();
        tetherScript = GetComponent<Prop>();

        playerLocation = GameObject.Find("ThirdPersonPlayer").transform;
    }

    public void Update()
    {
        //raycast from center of camera to get respawn position
        var x = Screen.width / 2;
        var y = Screen.height / 2;

        cameraCenterRay = camera.ScreenPointToRay(new Vector3(x, y, 0));
        Debug.DrawRay(cameraCenterRay.origin, cameraCenterRay.direction * 100, Color.yellow);
        if((spawnPosition - transform.position).magnitude >= spawnLimit)
        {
            print("TOO FAR FROM SPAWNPOINT");
            if ((spawnPosition - playerLocation.position).magnitude > playerRangeLimit){
                StartCoroutine(Respawn());
                print("TOO FAR FROM PLAYER");
            }
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Void")
        {
            StartCoroutine(Respawn());
            print("FELL INTO VOID");
        }
    }
    IEnumerator Respawn()
    {
        //Setup 
        rb.isKinematic = true;
        isFalling = true;
        tetherScript.enabled = false;

        //returnSpeedCurrent = 0;

        //play particle effect
        tinyTornado.Play();

        //MOVE TOWARDS ANCHOR POINT//   (this means i am moving the animal past the camera before it can go to its spawn)

        //move towards middle of screen position


        while (Vector3.Distance(transform.position, cameraCenterRay.GetPoint(distanceFromCameraView)) > returnCameraViewBuffer)
        {
            //accelerate overtime
            //Mathf.Clamp(returnSpeedCurrent,0,returnSpeedMax);
            //returnSpeedCurrent += returnAcceleration * Time.deltaTime; //accelerate return speed over time

            //move towards spawn position
            //var target = (Vector3.Distance(transform.position, cameraCenterRay.GetPoint(distanceFromCameraView)) > returnCameraViewBuffer);
            transform.position = Vector3.SmoothDamp(transform.position, cameraCenterRay.GetPoint(distanceFromCameraView), ref velocity, returnSpeed);

            // return when the playerview is true
            yield return null;
        }

        // DELAY TIMER//
        print("waiting");
        //returnSpeedCurrent = 0;
        yield return new WaitForSeconds(1f);


        // MOVE TOWARDS SPAWN POSITION //

        //check if it is close enough to spawn position
        var spawnDestination = new Vector3(spawnPosition.x, spawnPosition.y + respawnHeight, spawnPosition.z);

        while (Vector3.Distance(transform.position, spawnDestination) > returnBuffer)
        {
            //accelerate overtime
            //Mathf.Clamp(returnSpeedCurrent, 0, returnSpeedMax);
            //returnSpeedCurrent += returnAcceleration * Time.deltaTime; //accelerate return speed over time


            //move towards spawn position
            transform.position = Vector3.SmoothDamp(transform.position, spawnDestination, ref velocity, returnSpeed);

            // return when the result is null
            yield return null;
        }

        // DELAY TIMER//
        print("waiting");
        yield return new WaitForSeconds(1f);

        //RESET//
        print("reseting");

        //Enable Components
        rb.isKinematic = false;
        isFalling = false;
        tetherScript.enabled = true;

        //end particle effect
        tinyTornado.Stop();

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
