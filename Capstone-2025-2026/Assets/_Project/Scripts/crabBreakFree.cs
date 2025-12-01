using UnityEngine;

public class crabBreakFree : MonoBehaviour
{

    //sound 
    private AudioSource audioSource;
    public AudioClip popSound;

    //Components
    private Rigidbody rb;
    private Collider col;
    private Prop tetherScript;

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
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter(Collider collision)
    {

        //spike collision
        if (collision.gameObject.tag == "Spike")
        {
            Break();
            print("CRAB BROKE FREE");
        }
    }


    private void Break()
    {
        //add to count
        animalCounter.savedAnimals++;

        //play particle effect
        Instantiate(explodeParticle, transform.position, Quaternion.identity);

        isBroken = true;

        Destroy(gameObject);
    }
}
