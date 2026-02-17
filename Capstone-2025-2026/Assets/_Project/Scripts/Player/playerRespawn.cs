using FMODUnity;
using NodeCanvas.Tasks.Actions;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

public class PlayerRespawn : MonoBehaviour
{

    //Components
    private Rigidbody rb;
    [SerializeField] private ParticleSystem tinyTornado;

    //respawning
    private Vector3 spawnPosition;
    [SerializeField] private bool isFalling = false;
    [SerializeField] private float returnSpeed;
    [SerializeField] private float returnBuffer = 0.1f;
    [SerializeField] private float respawnHeight = 5f;

    [SerializeField] private RespawnPointVisuals currentRespawnPoint;

    //UI Components
    public ImageFader fadeToBlackScript;

    void Awake()
    {
        fadeToBlackScript.gameObject.SetActive(false);
    }

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
            if (currentRespawnPoint.firstTime)
            {
                //currentRespawnPoint.PlayFanfare();
            }
            currentRespawnPoint.SetObjectActive();
            spawnPosition = other.transform.position;
            print("Checkpoint Set!");
        }
    }

    IEnumerator Respawn()
    {
        //fade to black
        
        fadeToBlackScript.gameObject.SetActive(true);
        fadeToBlackScript.FadeIn();

        //Setup 
        rb.isKinematic = true;
        isFalling = true;

        //play particle effect
        tinyTornado.Play();
        //AudioManager.Instance.PlaySFX(AudioManager.Instance.PlayerSaved, 6, 1);
        RuntimeManager.PlayOneShot("event:/Respawn", transform.position);

        yield return new WaitUntil(() => fadeToBlackScript.FadeComplete);


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
        yield return new WaitForSeconds(.5f);

        //disable black screen
        fadeToBlackScript.FadeOut();
        yield return new WaitForSeconds(1f);

        //RESET//
        //Enable Components
        rb.isKinematic = false;
        isFalling = false;

        //end particle effect
        tinyTornado.Stop();

    }
}
