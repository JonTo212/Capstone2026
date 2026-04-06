using UnityEngine;

public class VelocityLimiter : MonoBehaviour
{
    public float maxVelocity = 10f; // Maximum linear velocity
    public float maxAngularVelocity = 20f; // Maximum angular velocity

    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.maxLinearVelocity = maxVelocity; // Limit movement speed
        rb.maxAngularVelocity = maxAngularVelocity; // Limit rotation speed
    }


}
