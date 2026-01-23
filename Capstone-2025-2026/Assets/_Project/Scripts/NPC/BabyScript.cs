using System;
using System.Collections;
using UnityEngine;

public class BabyScript : MonoBehaviour
{
    [SerializeField] private float timeInLight = 0f;
    [SerializeField] private float timetoSave = 3f;
    [SerializeField] private float returnSpeed = 0.1f;
    private Vector3 velocity; // need this for smoothdamp   


    //saving
    [SerializeField] private ParticleSystem explodeParticle;
    private Coroutine saveCoroutine;

    //components
    private Rigidbody rb;
    private Collider col;
    private Renderer materialObj;
    private Transform target;
    private Prop tetherScript;

    //materials
    [SerializeField] private Material cleanMat;
    [SerializeField] private Material corruptMat;

    //respawning
    private Vector3 spawnPosition;
    private ParticleSystem tinyTornado;

    [SerializeField] private float returnCameraViewBuffer = 0.1f;
    [SerializeField] private float returnBuffer = 0.1f;
    [SerializeField] private float respawnHeight = 5f;
    [SerializeField] private float distanceFromCameraView = 20f;
    [SerializeField] private float spawnLimit = 100f;
    [SerializeField] private float playerRangeLimit = 100f;
    private Ray cameraCenterRay;

    public event Action OnEnterBag;

    private void Start()
    {
        //change material to clean
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        tetherScript = GetComponent<Prop>();
        materialObj = GetComponent<Renderer>();


        //set spawn position
        spawnPosition = transform.position;
        tinyTornado = GetComponentInChildren<ParticleSystem>();
        materialObj.material = corruptMat;

        target = GameObject.FindGameObjectWithTag("Player").transform;
    }

    #region Unity Trigger Functions
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Void"))
        {
            StartCoroutine(Respawn());
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("LightGodRay"))
        {
            //countdown
            timeInLight += Time.fixedDeltaTime;

            //play particle effect
            if (!explodeParticle.isPlaying)
            {
                explodeParticle.Play();
            }

            //start save process
            if ((timeInLight >= timetoSave) && saveCoroutine == null)
            {
                saveCoroutine = StartCoroutine(Cleansed());
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("LightGodRay"))
        {
            //reset countdown
            timeInLight = 0f;

            //stop particle effect
            explodeParticle.Stop();
        }
    }
    #endregion

    #region Respawning
    private IEnumerator Respawn()
    {
        //setup 
        rb.isKinematic = true;
        tetherScript.enabled = false;

        //play particle effect
        tinyTornado.Play();

        //MOVE TOWARDS ANCHOR POINT//   (this means i am moving the animal past the camera before it can go to its spawn)
        //move towards middle of screen position
        while (Vector3.Distance(transform.position, cameraCenterRay.GetPoint(distanceFromCameraView)) > returnCameraViewBuffer)
        {
            transform.position = Vector3.SmoothDamp(transform.position, cameraCenterRay.GetPoint(distanceFromCameraView), ref velocity, returnSpeed);
            yield return null;
        }

        // DELAY TIMER//
        yield return new WaitForSeconds(1f);


        // MOVE TOWARDS SPAWN POSITION //
        //check if it is close enough to spawn position
        var spawnDestination = new Vector3(spawnPosition.x, spawnPosition.y + respawnHeight, spawnPosition.z);

        //move towards spawn position
        while (Vector3.Distance(transform.position, spawnDestination) > returnBuffer)
        {
            transform.position = Vector3.SmoothDamp(transform.position, spawnDestination, ref velocity, returnSpeed);
            yield return null;
        }

        // DELAY TIMER//
        yield return new WaitForSeconds(1f);

        //RESET//
        //Enable Components
        rb.isKinematic = false;
        tetherScript.enabled = true;

        //end particle effect
        tinyTornado.Stop();
    }
    #endregion

    #region Cleansing
    private IEnumerator Cleansed()
    {
        var returnBuffer = 0.5f;

        //disable components
        col.enabled = false;    
        rb.isKinematic = true;
        tetherScript.enabled = false;

        //fanfare sound
        AudioManager.Instance.PlaySFX(AudioManager.Instance.FanFare, 5, 5);

        //change material to clean
        GetComponent<Renderer>().material = cleanMat;

        //stop cleansing particle
        explodeParticle.Stop();

        //Pose
        var posePosition = transform.position + new Vector3(5f, 5f, 0);
        while (Vector3.Distance(transform.position, posePosition) > returnBuffer)
        {
            //face player
            transform.LookAt(target.position);
            transform.position = Vector3.Lerp(transform.position, posePosition, returnSpeed * Time.deltaTime);
            yield return null;
        }

        yield return new WaitForSeconds(2f);


        //go to player backpack
        while (Vector3.Distance(transform.position, target.position) > returnBuffer)
        {
            transform.position = Vector3.Lerp(transform.position, target.position, returnSpeed * Time.deltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, returnSpeed * Time.deltaTime);
            yield return null;
        }

        EnterBag();
    }

    public void EnterBag()
    {
        //collect sound
        AudioManager.Instance.PlaySFX(AudioManager.Instance.Collection, 5, 5);
        OnEnterBag.Invoke();
        Destroy(gameObject);
    }
    #endregion
}
