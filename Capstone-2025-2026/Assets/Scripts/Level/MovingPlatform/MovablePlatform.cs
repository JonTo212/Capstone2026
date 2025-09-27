using UnityEngine;
using UnityEngine.XR;

public class MovablePlatform : MonoBehaviour
{
    public Transform player;
    private Tetherable tetherable;
    private Rigidbody rb;
    private Vector3 lastPosition;
    private Vector3 currentPosition;
    private Vector3 previousSpeed;
    private Vector3 currentSpeed;


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
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        if(player != null && playerHasBeenLaunced == false)
        {
            player.GetComponent<Rigidbody>().AddForce(tetherable.ForceBeingReceived, ForceMode.Force);

            //Debug.Log("Player force");

            //CheckForlaunchMinimumSpeed();

            //if(DidAbruptlyStop())
            //{
            //    Debug.Log("Check");

            //    LaunchPlayer();

            //    player = null;
            //}
        }


        //Vector3 delta = transform.position - lastPosition;

        //player.gameObject.GetComponent<Rigidbody>().MovePosition(delta);

        //lastPosition = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Player" && playerHasBeenLaunced == false)
        {
            player = other.transform;
            //player.GetComponent<Rigidbody>().linearVelocity = GetComponent<Rigidbody>().linearVelocity;
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
            //rb.isKinematic = true;
        }
    }

    private void CheckForlaunchMinimumSpeed()
    {
        if(rb.linearVelocity.magnitude > minimumSpeedToLaunch)
        {
            hasReachedSpeedToLaunch = true;

            player.GetComponent<Rigidbody>().linearVelocity = rb.linearVelocity;
        }
        else
        {
            hasReachedSpeedToLaunch = false;
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
