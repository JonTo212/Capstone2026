using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    private PlayerMovement playerRef;
    private Rigidbody rb;
    private float previousFrameVelocityMagnitude;
    [SerializeField, Range(0,1)] private float stickiness;
    [SerializeField, Range(0, 1)] private float jumpOnCrashMultiplier = 0.8f;
    [SerializeField] private float crashSpeedThreshold = 5f;

    private void Awake()
    {
        rb = GetComponentInParent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (playerRef != null)
        {
            if(CheckCrash())
            {
                DisconnectPlayer(true);
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
        if(other.gameObject.CompareTag("Player"))
        {
            playerRef = other.GetComponent<PlayerMovement>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            DisconnectPlayer(false);
        }
    }

    private void DisconnectPlayer(bool jump)
    {
        playerRef.InheritPlatformMomentum(Vector3.zero);
        if (jump) playerRef.Jump(jumpOnCrashMultiplier, true);
        playerRef = null;
        rb.interpolation = RigidbodyInterpolation.None;
    }

    private bool CheckCrash()
    {
        float speedLoss = previousFrameVelocityMagnitude - rb.linearVelocity.magnitude;
        if (speedLoss > crashSpeedThreshold) return true;
        return false;
    }
}
