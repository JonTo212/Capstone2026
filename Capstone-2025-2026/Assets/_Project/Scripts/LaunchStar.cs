using System.Collections;
using UnityEngine;

public class LaunchStar : MonoBehaviour
{
    //README THIS LAUNCH STAR SCRIPT IS USING SAME LOGIC AS ANIMAL RESPAWN FOR TESTING PURPOSES. WILL NEED TO UPDATE LATER

    public GameObject player;

    //Components
    private Rigidbody rb;

    //respawning
    public Transform spawnPosition;
    public bool isFalling = false;
    public float returnBuffer = 0.1f;

    public float returnSpeed;// time in seconds it takes to reach destination
    private Vector3 velocity; // need this for smoothdamp

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = player.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(Respawn());
        }
    }


    IEnumerator Respawn()
    {
        //Setup 
        rb.isKinematic = true;
        isFalling = true;

        //returnSpeedCurrent = 0;


        // DELAY TIMER//
        print("waiting");
        //returnSpeedCurrent = 0;
        yield return new WaitForSeconds(.5f);


        // MOVE TOWARDS SPAWN POSITION //

        //check if it is close enough to spawn position
        var spawnDestination = new Vector3(spawnPosition.position.x, spawnPosition.position.y, spawnPosition.position.z);

        while (Vector3.Distance(player.transform.position, spawnDestination) > returnBuffer)
        {
            //accelerate overtime
            //Mathf.Clamp(returnSpeedCurrent, 0, returnSpeedMax);
            //returnSpeedCurrent += returnAcceleration * Time.deltaTime; //accelerate return speed over time


            //move towards spawn position
            player.transform.position = Vector3.SmoothDamp(player.transform.position, spawnDestination, ref velocity, returnSpeed);

            // return when the result is null
            yield return null;
        }

        // DELAY TIMER//
        print("waiting");
        yield return new WaitForSeconds(.5f);

        //RESET//
        print("reseting");

        //Enable Components
        rb.isKinematic = false;
        isFalling = false;


    }
}
