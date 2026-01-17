using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    private PlayerMovement playerRef;
    [SerializeField] private Rigidbody rb;
    [SerializeField, Range(0,1)] private float stickiness;

    private void FixedUpdate()
    {
        if (playerRef != null)
        {
            playerRef.ExternalForce = rb.linearVelocity * stickiness;
            if (rb.interpolation != RigidbodyInterpolation.Interpolate)
            {
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Player"))
        {
            playerRef = other.GetComponent<PlayerMovement>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            playerRef.InheritPlatformMomentum(rb.linearVelocity);
            playerRef = null;
            rb.interpolation = RigidbodyInterpolation.None;
        }
    }
}
