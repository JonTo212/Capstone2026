using UnityEngine;

public class PlayerSwing : MonoBehaviour
{
    private PlayerMovement playerMovement;
    private Vector3 swingPoint;

    private float ropeLength;
    [SerializeField] private Camera playerCam;
    [SerializeField] private float minRopeLength;
    [SerializeField] private float maxRopeLength;
    [SerializeField] private float airAccel;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    public void StartSwing(Vector3 anchorPoint, Vector3 startingVel, float startingLength)
    {
        swingPoint = anchorPoint;
        ropeLength = Mathf.Clamp(startingLength, minRopeLength, maxRopeLength);

        Vector3 ropeDir = (swingPoint - transform.position).normalized;
        Vector3 tangentialVel = Vector3.ProjectOnPlane(startingVel, ropeDir);

        playerMovement.Rb.linearVelocity = tangentialVel;
    }

    public void HandleSwingMovement(Vector3 moveDir, ref Vector3 relVel)
    {
        Vector3 directionToGrapple = swingPoint - transform.position;
        Vector3 ropeDir = directionToGrapple.normalized;

        Vector3 tangentialMoveDir = Vector3.ProjectOnPlane(moveDir, ropeDir);
        relVel += tangentialMoveDir * airAccel * Time.fixedDeltaTime;
    }

    public void ConstrainToRope(ref Vector3 relVel)
    {
        Vector3 directionToGrapple = swingPoint - transform.position;
        float currentDistance = directionToGrapple.magnitude;

        if (currentDistance > ropeLength)
        {
            Vector3 ropeDir = directionToGrapple.normalized;

            Vector3 velocityAlongRope = Vector3.Project(relVel, ropeDir);

            float stretch = currentDistance - ropeLength;
            float stiffness = 25f; 
            float damping = 2f * Mathf.Sqrt(stiffness * playerMovement.Rb.mass);

            Vector3 correctiveForce = ropeDir * (stretch * stiffness);
            correctiveForce -= velocityAlongRope * damping;

            relVel += correctiveForce * Time.fixedDeltaTime;
        }
    }

    public void SetRopeLength(float newRopeLength)
    {
        ropeLength = newRopeLength;
    }

    public void UpdateAnchorPoint(Vector3 anchorPoint)
    {
        swingPoint = anchorPoint;
    }


    /* //this is the positional version, as posted in the spiderman 2 swinging
    public void Swing(Vector3 moveDir, float gravity, float friction)
    { 
        Vector3 inputForce = moveDir * airAccel;
        Vector3 gravityForce = Vector3.down * gravity;
        Vector3 dragForce = -velocity * friction;

        Vector3 accel = inputForce + gravityForce + dragForce;

        velocity += accel * Time.fixedDeltaTime;

        Vector3 currentPos = transform.position;
        Vector3 newPos = currentPos + velocity * Time.fixedDeltaTime;

        float distance = Vector3.Distance(newPos, swingPoint);
        if (distance > ropeLength)
        {
            Vector3 dir = (newPos - swingPoint).normalized;
            newPos = swingPoint + (dir * ropeLength);
        }

        velocity = (newPos - currentPos) / Time.fixedDeltaTime;
        playerMovement.Rb.linearVelocity = velocity;
    }*/
}
