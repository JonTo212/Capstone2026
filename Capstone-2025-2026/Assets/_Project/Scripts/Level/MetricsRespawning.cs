using UnityEngine;

public class MetricsRespawning : MonoBehaviour
{
    private Vector3 spawnPosition;
    public float returnBuffer = 5f;

    public float timerMax = 5f;
    public float currentTime;

    private Prop propScript;
    private Rigidbody rb;
   // public GameObject respawnParticle;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //set spawn position
        spawnPosition = transform.position;

        //timer
        currentTime = timerMax;

        //get prop script
        propScript = GetComponent<Prop>();

        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (propScript.IsSnared == false && (Vector3.Distance(transform.position, spawnPosition) > returnBuffer) && rb.linearVelocity.magnitude < 1f )
        {
            Countdown();
        }
        else
        {
            currentTime = timerMax;
        }

        //print(rb.linearVelocity.magnitude);
    }

    void Countdown()
    {
        currentTime -= Time.deltaTime;

        if (currentTime <= 0f)
        {
            Respawn();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == ("Void"))
        {
            Respawn();
        }
    }

    void Respawn()
    {

        //Instantiate(respawnParticle, spawnPosition, Quaternion.identity);
        rb.linearVelocity = Vector3.zero;
        transform.position = spawnPosition;
        currentTime = timerMax;
    }



}
