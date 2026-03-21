using UnityEngine;
using UnityEngine.Splines;

public class PlayerSwing : MonoBehaviour
{
    private PlayerRefData _playerRefData;

    private Vector3 swingPoint;
    private float ropeLength;

    [SerializeField] private float startRopeLength;
    [SerializeField] private float minRopeLength;
    [SerializeField] private float maxRopeLength;

    [SerializeField] private float airAccel;
    [SerializeField] private float swingJumpForce;

    public float AttachLength { get; private set; }

    private void Awake()
    {
        _playerRefData = GetComponent<PlayerRefData>();
    }

    public void SwingJumpBoost()
    {
        _playerRefData.Lasso.HandleObjectReleased();
        _playerRefData.PlayerMovement.Rb.AddForce(Vector3.up * swingJumpForce, ForceMode.Impulse);
    }

    public void StartSwing(Vector3 anchorPoint, Vector3 startingVel, float startingLength)
    {
        swingPoint = anchorPoint;
        //ropeLength = Mathf.Clamp(startingLength, minRopeLength, maxRopeLength);
        ropeLength = Mathf.Clamp(startRopeLength, minRopeLength, maxRopeLength);

        Vector3 ropeDir = (swingPoint - transform.position).normalized;
        Vector3 tangentialVel = Vector3.ProjectOnPlane(startingVel, ropeDir);

        _playerRefData.PlayerMovement.Rb.linearVelocity = tangentialVel;
    }

    public void HandleSwingMovement(Vector3 moveDir, ref Vector3 relVel)
    {
        Vector3 directionToGrapple = swingPoint - transform.position;
        Vector3 ropeDir = directionToGrapple.normalized;

        Vector3 displacement = relVel * Time.fixedDeltaTime;

        Vector3 tangentialMoveDir = Vector3.ProjectOnPlane(moveDir, ropeDir);
        relVel += tangentialMoveDir * airAccel * Time.fixedDeltaTime;

        if (_playerRefData.PlayerMovement.Rb.SweepTest(displacement.normalized, out RaycastHit hit, displacement.magnitude * 2f, QueryTriggerInteraction.Ignore))
        {
            relVel = Vector3.ProjectOnPlane(relVel, hit.normal);
        }
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
            float damping = 2f * Mathf.Sqrt(stiffness * _playerRefData.PlayerMovement.Rb.mass);

            Vector3 correctiveForce = ropeDir * (stretch * stiffness);
            correctiveForce -= velocityAlongRope * damping;

            relVel += correctiveForce * Time.fixedDeltaTime;
        }
    }

    public void AdjustRopeLength(float scrollInput)
    {
        if (Mathf.Approximately(scrollInput, 0f)) return;

        ropeLength += scrollInput;
        ropeLength = Mathf.Clamp(ropeLength, minRopeLength, maxRopeLength);
    }
}
