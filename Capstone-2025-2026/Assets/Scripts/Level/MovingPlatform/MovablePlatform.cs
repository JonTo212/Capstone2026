using UnityEngine;
using UnityEngine.XR;

public class MovablePlatform : MonoBehaviour
{
    [SerializeField] bool canLaunchPlayer = false;

    public Transform player;
    private Tetherable tetherable;
    private Rigidbody rb;
    private Vector3 lastPosition;
    private Vector3 currentPosition;
    private Vector3 previousSpeed;
    private Vector3 currentSpeed;
    public GameObject previousCollision;

    public AudioClip crackingNoise;
    public AudioClip islandHitClip;
    public AudioSource crackingNoiseSource;
    public AudioSource islandHitSource;

    [SerializeField] bool hasReachedSpeedToLaunch = false;
    bool playerHasBeenLaunced = false;
    [SerializeField] private float minimumSpeedToLaunch = 10f;
    [SerializeField] private float minmumSpeedDecreaseToLaunch = 10f;
    [SerializeField] private float launchHeightStrength = 5f;
    [SerializeField] private float launchSpeedStrength = 2f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        tetherable = GetComponent<Tetherable>();
        rb = GetComponent<Rigidbody>();

        crackingNoiseSource = gameObject.AddComponent<AudioSource>();
        crackingNoiseSource.playOnAwake = false;
        crackingNoiseSource.clip = crackingNoise;
        crackingNoiseSource.loop = true;
        crackingNoiseSource.minDistance = 25;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        if(player != null && playerHasBeenLaunced == false)
        {
            player.GetComponent<Rigidbody>().AddForce(rb.linearVelocity, ForceMode.Force);

            Debug.Log("Player force");

            CheckForlaunchMinimumSpeed();

            if (DidAbruptlyStop() && canLaunchPlayer)
            {
                Debug.Log("Check");

                LaunchPlayer();

                player = null;
            }
        }

        lastPosition = transform.position;

        if(rb.linearVelocity.magnitude > 0.15f)
        {
            if(!crackingNoiseSource.isPlaying)
            {
                crackingNoiseSource.Play();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Player" && playerHasBeenLaunced == false)
        {
            player = other.transform;
            player.GetComponent<Rigidbody>().linearVelocity = GetComponent<Rigidbody>().linearVelocity;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            player = null;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag != "Player")
        {
            if(previousCollision != collision.gameObject)
            {
                crackingNoiseSource.Stop();
                crackingNoiseSource.PlayOneShot(islandHitClip);
                previousCollision = collision.gameObject;
                rb.isKinematic = true;
            }
        }

        OverlapPlayerCameraShake();
    }

    private void OverlapPlayerCameraShake()
    {
        Collider[] objectsNear = Physics.OverlapSphere(transform.position, 10);

        foreach(Collider collider in objectsNear)
        {
            if(collider.gameObject.tag == "Player")
            {
                CameraShake shake = Camera.main.gameObject.GetComponent<CameraShake>();
                StartCoroutine(shake.Shake(0.3f, 0.2f));
            }
        }
    }

    private void CheckForlaunchMinimumSpeed()
    {
        if(rb.linearVelocity.magnitude > minimumSpeedToLaunch)
        {
            hasReachedSpeedToLaunch = true;

            player.GetComponent<Rigidbody>().linearVelocity = rb.linearVelocity;
        }
    }

    private bool DidAbruptlyStop()
    {
        if(hasReachedSpeedToLaunch)
        {
            if((previousSpeed.magnitude - rb.linearVelocity.magnitude) > minimumSpeedToLaunch)
            {
                previousSpeed = rb.linearVelocity;

                return true;
            }
        }

        previousSpeed = rb.linearVelocity;

        return false;
    }

    private void LaunchPlayer()
    {
        player.GetComponent<Rigidbody>().AddForce(player.forward * launchSpeedStrength, ForceMode.VelocityChange);

        player.GetComponent<Rigidbody>().AddForce(Vector3.up * launchHeightStrength, ForceMode.VelocityChange);

        playerHasBeenLaunced = true;
    }
}
