using UnityEngine;

public class NoGravityForce : MonoBehaviour
{
    private Rigidbody rb;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        rb.useGravity = false;

        //set velocity to zero
        rb.linearVelocity = Vector3.zero;

        //disable all rotation
        rb.angularVelocity = Vector3.zero;

    }
}
