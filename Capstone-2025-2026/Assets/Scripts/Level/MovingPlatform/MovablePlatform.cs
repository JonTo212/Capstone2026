using UnityEngine;

public class MovablePlatform : MonoBehaviour
{
    public Transform player;
    private Tetherable tetherable;
    private Vector3 lastPosition;
    private Vector3 currentPosition;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        tetherable = GetComponent<Tetherable>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        if(player != null)
        {
            player.GetComponent<Rigidbody>().AddForce(tetherable.ForceBeingReceived, ForceMode.Force);

            //Debug.Log("Player force");
        }

        //Vector3 delta = transform.position - lastPosition;

        //player.gameObject.GetComponent<Rigidbody>().MovePosition(delta);

        //lastPosition = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Player")
        {
            player = other.transform;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            player = null;
        }
    }
}
