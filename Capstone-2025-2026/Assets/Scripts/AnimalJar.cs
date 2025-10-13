using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;

public class AnimalJar : MonoBehaviour
{
    //bubble animation
    public GameObject bubble;

    //sound 
    private AudioSource audioSource;
    public AudioClip popSound;

    //Components
    private Rigidbody rb;
    private Collider col;
    private Tetherable tetherScript;

    //particles
    public GameObject explodeParticle;

    private ParticleSystem tinyTornado;

    //Break Free
    private GameObject trappedAnimal;
    public bool isBroken = false;

    //respawning
    private Vector3 spawnPosition;
    public bool isFalling = false;

    public float returnCameraViewBuffer = 0.1f;
    public float returnBuffer = 0.1f;
    public float respawnHeight = 5f;

    public float returnSpeed;// time in seconds it takes to reach destination
    //public float returnAcceleration;
    //public float returnSpeedMax;
    private Vector3 velocity; // need this for smoothdamp

    //vamera view stuff
    Camera camera;
    private Ray cameraCenterRay;
    public float distanceFromCameraView = 20f;



    private void Start()
    {
        //get the trapped animal object in jar
        if (transform.childCount > 0)
        {
            trappedAnimal = transform.GetChild(0).gameObject;
        }

        //set spawn position
        spawnPosition = transform.position;

        //get Components
        rb = GetComponent<Rigidbody>();
        col = GetComponent<SphereCollider>();
        tinyTornado = GetComponentInChildren<ParticleSystem>();
        audioSource = GetComponent<AudioSource>();
        tetherScript = GetComponent<Tetherable>();

        camera = Camera.main;
    }

    public void Update()
    {
        //raycast from center of camera to get respawn position
        var x = Screen.width / 2;
        var y = Screen.height / 2;

        cameraCenterRay = camera.ScreenPointToRay(new Vector3(x, y, 0));
        Debug.DrawRay(cameraCenterRay.origin, cameraCenterRay.direction * 100, Color.yellow);
    }

    private void OnCollisionEnter(Collision collision)
    {
        //bubble animation
        var animator = bubble.GetComponent<Animator>();

        if (animator != null)
        {
            animator.Play("bubbleBounceAnim",0,0);
        }

        //spike collision
        if (collision.gameObject.tag == "Spike")
        {
            Break();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Void")
        {
            StartCoroutine(Respawn());
            print("FELL INTO VOID");
        }
    }


    private void Break()
    {
        //add to count
        animalCounter.savedAnimals++;

        //play particle effect
        Instantiate(explodeParticle, transform.position, Quaternion.identity);

        isBroken = true;

        //separate animal from jar
        if (trappedAnimal != null)
        {
            trappedAnimal.transform.SetParent(null);
        }

        Destroy(gameObject);
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
        var spawnDestination = new Vector3 (spawnPosition.x,spawnPosition.y+respawnHeight,spawnPosition.z);

        while (Vector3.Distance(transform.position, spawnDestination) > returnBuffer)
        {
            //accelerate overtime
            //Mathf.Clamp(returnSpeedCurrent, 0, returnSpeedMax);
            //returnSpeedCurrent += returnAcceleration * Time.deltaTime; //accelerate return speed over time


            //move towards spawn position
            transform.position = Vector3.SmoothDamp(transform.position, spawnDestination, ref velocity,returnSpeed);

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

}
