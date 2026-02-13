using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;
using NodeCanvas.Tasks.Actions;

public class BridgeBreakSetpiece : MonoBehaviour
{

    //Components
    [SerializeField] private GameObject player;
    private Rigidbody rb;

    //respawning
    public Transform spawnPosition;
    [SerializeField] private bool isFalling = false;
    [SerializeField] private float returnSpeed;
    [SerializeField] private float returnBuffer = 0.1f;
    [SerializeField] private float respawnHeight = 5f;


    public GameObject bridge;

    //UI Components
    public ImageFader fadeToBlackScript;

    void Awake()
    {
        fadeToBlackScript.gameObject.SetActive(false);
    }

    void Start()
    {

        //get Components
        player = GameObject.FindWithTag("Player");
        rb = player.GetComponent<Rigidbody>();
    }



    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            StartCoroutine(BridgeBreak());
            print("bridgeBroken");
        }
    }

    IEnumerator BridgeBreak()
    {
        yield return new WaitForSeconds(1f);
        Destroy(bridge);

        yield return new WaitForSeconds(2f);
        //Setup 
        rb.isKinematic = true;
        isFalling = true;

        //fade to black

        fadeToBlackScript.gameObject.SetActive(true);
        fadeToBlackScript.FadeIn();


        yield return new WaitUntil(() => fadeToBlackScript.FadeComplete);


        // MOVE TOWARDS SPAWN POSITION //

        //check if it is close enough to spawn position
        var spawnDestination = new Vector3(spawnPosition.position.x, spawnPosition.position.y, spawnPosition.position.z);

        while (Vector3.Distance(player.transform.position, spawnDestination) > returnBuffer)
        {

            //move towards spawn position
            player.transform.position = spawnDestination;

            // return when the result is null
            yield return null;
        }

        // DELAY TIMER//
        yield return new WaitForSeconds(.5f);

        //disable black screen
        fadeToBlackScript.FadeOut();

        //RESET//
        //Enable Components
        rb.isKinematic = false;
        isFalling = false;
    }
}
