using Unity.VisualScripting;
using UnityEngine;

public class bouncetest : MonoBehaviour
{
    public bool bounceActivate = false;
    public float bounceForce = 100f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("bounceRock"))
        {
            bounceActivate = true;

            //teleport far away
            other.transform.position = new Vector3(0, -100000, 0);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent(out Rigidbody rb))
        {
            if (bounceActivate)
            {
                // Apply an upward force to the rigidbody
                rb.AddForce(Vector3.up * bounceForce);

                //apply slight rotation force
                rb.AddTorque(Vector3.right * (bounceForce / 100f));

                print("Bounce applied to " + other.name);
                //bounceActivate = false;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("bounceRock"))
        {
            bounceActivate = false;
        }
    }
}
