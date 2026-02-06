using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    private PlayerMovement playerRef;
    private Rigidbody rb;
    [SerializeField, Range(0, 1)] private float stickiness;
    private float previousFrameVelocityMagnitude;
    private bool crashed;

    private void Awake()
    {
        rb = GetComponentInParent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (playerRef != null)
        {
            if (CheckCrash())
            {
                DisconnectPlayer();
                return;
            }

            playerRef.SetExternalForce(rb.linearVelocity * stickiness);
            if (rb.interpolation != RigidbodyInterpolation.Interpolate)
            {
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }

        previousFrameVelocityMagnitude = rb.linearVelocity.magnitude;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            playerRef = other.GetComponent<PlayerMovement>();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.CompareTag("Player") && playerRef == null)
        {
            playerRef = other.GetComponent<PlayerMovement>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            DisconnectPlayer();
        }
    }

    private void DisconnectPlayer()
    {
        playerRef.InheritPlatformMomentum(Vector3.zero);
        playerRef = null;
        rb.interpolation = RigidbodyInterpolation.None;
    }

    [SerializeField] private float crashSpeedThreshold = 5f;

    private bool CheckCrash()
    {
        float speedLoss = previousFrameVelocityMagnitude - rb.linearVelocity.magnitude;
        if (speedLoss > crashSpeedThreshold) return true;
        return false;
    }
}
