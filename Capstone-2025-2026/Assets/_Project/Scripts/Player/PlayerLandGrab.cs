using UnityEngine;

public class PlayerLandGrab : MonoBehaviour
{
    [SerializeField] private float grabCheckDistance = 1f;
    [SerializeField] private LayerMask grabbableMask;

    private PlayerActions playerInput;
    private PlayerMovement playerController;
    private bool isGrabbing;
    private Rigidbody attachedObjBody;

    private void Awake()
    {
        playerInput = GetComponent<PlayerActions>();
        playerController = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        Transform nearestGrabbable = null;
        if (playerInput.grabHeld && CanGrab(out nearestGrabbable))
        {
            if (!isGrabbing)
            {
                HandleGrabStart(nearestGrabbable);
            }
        }
        if(playerInput.grabUp)
        {
            HandleGrabEnd();
        }
    }

    private void FixedUpdate()
    {
        if(attachedObjBody != null && isGrabbing)
            HandleGrab();
    }

    private bool CanGrab(out Transform closestGrabbable)
    {
        //check everything around the player
        Collider[] hits = Physics.OverlapSphere(transform.position, grabCheckDistance, grabbableMask);

        if (hits.Length == 0)
        {
            closestGrabbable = null;
            return false;
        }

        //get closest grabbable
        Collider closest = null;
        float closestDist = float.MaxValue;

        foreach (var col in hits)
        {
            float dist = Vector3.Distance(transform.position, col.ClosestPoint(transform.position));
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = col;
            }
        }

        if (closest == null)
        {
            closestGrabbable = null;
            return false;
        }

        Vector3 closestPoint = closest.ClosestPoint(transform.position);
        closestGrabbable = closest.transform;
        return true;
    }

    private void HandleGrabStart(Transform anchorObject)
    {
        playerInput.ChangeSpecificInput("Move", false);

        if(anchorObject.TryGetComponent(out Rigidbody attachedRb))
        {
            attachedObjBody = attachedRb;
        }
        else
        {
            playerController.Rb.isKinematic = true;
            //transform.SetParent(anchorObject.transform, true);
        }

        playerController.SetGrabbing(true);
        isGrabbing = true;
    }

    private void HandleGrab()
    {
        attachedObjBody.interpolation = RigidbodyInterpolation.Interpolate;
        playerController.Rb.linearVelocity = attachedObjBody.linearVelocity;
    }

    private void HandleGrabEnd()
    {
        playerInput.ChangeSpecificInput("Move", true);

        if (attachedObjBody != null)
        {
            playerController.InheritPlatformMomentum(attachedObjBody.linearVelocity);
            playerController.ExternalForce = Vector3.zero;
            attachedObjBody = null;
        }
        else
        {
            playerController.Rb.isKinematic = false;
            //transform.SetParent(null);
        }

        playerController.SetGrabbing(false);
        isGrabbing = false;
    }
}
