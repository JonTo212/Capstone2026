using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;
using NodeCanvas.Tasks.Actions;

public class playerRespawn : MonoBehaviour
{

    //Components
    private Rigidbody rb;
    public ParticleSystem tinyTornado;

    //respawning
    private Vector3 spawnPosition;
    public bool isFalling = false;
    public float returnSpeed;
    public float returnBuffer = 0.1f;
    public float respawnHeight = 5f;

    public bool inPlayerView = false;
    public GameObject playerViewAnchor;
    public RespawnPointVisuals currentRespawnPoint;
    AudioManager aManage;

    //UI Components
    public FadeToBlack fadeToBlackScript;

    void Awake()
    {
        aManage = GameObject.Find("AudioManager").GetComponent<AudioManager>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //set spawn position
        spawnPosition = transform.position;

        //get Components
        rb = GetComponent<Rigidbody>();
    }



    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Void")
        {
            StartCoroutine(Respawn());
            print("FELL INTO VOID");
        }


        if (other.gameObject.tag == "Checkpoint")
        {
            if (currentRespawnPoint != null)
            {
                currentRespawnPoint.SetObjectActive();
            }
            currentRespawnPoint = other.GetComponent<RespawnPointVisuals>();
            currentRespawnPoint.SetObjectActive();
            spawnPosition = other.transform.position;
            print("Checkpoint Set!");
        }
    }

    IEnumerator Respawn()
    {
        //fade to black
        fadeToBlackScript.blackOut = true;

        //Setup 
        rb.isKinematic = true;
        isFalling = true;

        //play particle effect
        tinyTornado.Play();
        aManage.PlaySFX(aManage.PlayerSaved, 6, 1);

        yield return new WaitUntil(() => fadeToBlackScript.fullBlack == true);


        // MOVE TOWARDS SPAWN POSITION //

        //check if it is close enough to spawn position
        var spawnDestination = new Vector3(spawnPosition.x, spawnPosition.y + respawnHeight, spawnPosition.z);

        while (Vector3.Distance(transform.position, spawnDestination) > returnBuffer)
        {

            //move towards spawn position
            transform.position = spawnDestination;

            // return when the result is null
            yield return null;
        }

        // DELAY TIMER//
        print("waiting");

        yield return new WaitForSeconds(.5f);
        //disable black screen
        fadeToBlackScript.blackOut = false;
        yield return new WaitForSeconds(1f);

        //RESET//
        print("reseting");

        //Enable Components
        rb.isKinematic = false;
        isFalling = false;
        inPlayerView = false;

        //end particle effect
        tinyTornado.Stop();

    }
}
