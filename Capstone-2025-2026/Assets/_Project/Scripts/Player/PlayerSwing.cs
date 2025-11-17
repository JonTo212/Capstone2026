using UnityEngine;

public class PlayerSwing : MonoBehaviour
{
    private PlayerActions playerInput;
    private PlayerMovement playerMovement;
    private CapsuleCollider col;

    private Vector3 velocity;
    private Vector3 swingPoint;

    private float ropeLength;
    [SerializeField] private Camera playerCam;
    [SerializeField] private float minRopeLength;
    [SerializeField] private float maxRopeLength;
    [SerializeField] private float airAccel;

    private void Awake()
    {
        playerInput = GetComponent<PlayerActions>();
        playerMovement = GetComponent<PlayerMovement>();
        col = GetComponent<CapsuleCollider>();
    }


    public void StartSwing(Vector3 anchorPoint, Vector3 startingVel, float startingLength)
    {
        swingPoint = anchorPoint;
        ropeLength = Mathf.Clamp(startingLength, minRopeLength, maxRopeLength);

        Vector3 ropeDir = (swingPoint - transform.position).normalized;
        Vector3 tangentialVel = Vector3.ProjectOnPlane(startingVel, ropeDir);

        velocity = tangentialVel;
        playerMovement.Rb.linearVelocity = tangentialVel;
    }

    public void HandleSwingMovement(Vector3 moveDir)
    {
        Vector3 directionToGrapple = swingPoint - transform.position;
        Vector3 ropeDir = directionToGrapple.normalized;

        Vector3 rbVel = playerMovement.Rb.linearVelocity;
        Vector3 tangentialVel = Vector3.ProjectOnPlane(rbVel, ropeDir);

        Vector3 tangentialMoveDir = Vector3.ProjectOnPlane(moveDir, ropeDir);

        Vector3 swingForce = tangentialMoveDir * airAccel;

        playerMovement.Rb.AddForce(swingForce, ForceMode.Acceleration);

        ConstrainToRope();
    }

    public void ConstrainToRope()
    {
        Vector3 directionToGrapple = swingPoint - transform.position;
        float currentDistance = directionToGrapple.magnitude;
        Vector3 ropeDir = directionToGrapple.normalized;
        Vector3 velocityAlongRope = Vector3.Project(playerMovement.Rb.linearVelocity, ropeDir);

        if (currentDistance != ropeLength)
        {
            float stretch = currentDistance - ropeLength;

            float stiffness = 25f;
            float damping = 2f * Mathf.Sqrt(stiffness * playerMovement.Rb.mass);

            //pull towards rope length with spring/damper
            Vector3 correctiveForce = ropeDir * (stretch * stiffness);  
            correctiveForce -= velocityAlongRope * damping;

            playerMovement.Rb.AddForce(correctiveForce, ForceMode.Acceleration);
        }
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
