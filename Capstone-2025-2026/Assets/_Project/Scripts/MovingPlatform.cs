using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    private PlayerMovement playerRef;
    [SerializeField] private Rigidbody rb;

    private void Update()
    {
        if (playerRef != null)
        {
            playerRef.ExternalForce = rb.linearVelocity;
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
            playerRef.ExternalForce = Vector3.zero;
            playerRef = null;
            rb.interpolation = RigidbodyInterpolation.None;
        }
    }
}
