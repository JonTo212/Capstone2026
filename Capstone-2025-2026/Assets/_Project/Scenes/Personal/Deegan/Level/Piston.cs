using DG.Tweening;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;

public enum PistonRailAxis
{
    X, Y, Z
}

public class Piston : MonoBehaviour
{
    [Header("Properties")]
    public Transform startPosition;
    public Transform endPosition;
    public LineRenderer lineRenderer;
    public PistonRailAxis axis = PistonRailAxis.X;
    private Rigidbody rb;
    private float maxPosition = 0f;
    private float minPosition = 0f;
    private float currentPositionAlongRail = 0f;

    [Header("Spring Properties")]
    public bool hasSpring = false;
    public float springForce = 50f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (axis == PistonRailAxis.X) rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ;
        if (axis == PistonRailAxis.Y) rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
        if (axis == PistonRailAxis.Z) rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY;

        lineRenderer = GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    void Update()
    {

        lineRenderer.SetPosition(0, startPosition.position);
        lineRenderer.SetPosition(1, endPosition.position);
    }

    private void FixedUpdate()
    {
        ResetSpeedAtEdge();
        ConstrainPosition();
    }

    private void ConstrainPosition()
    {
        if (axis == PistonRailAxis.X)
        {
            if (startPosition.position.x > endPosition.position.x)
            {
                maxPosition = startPosition.position.x;
                minPosition = endPosition.position.x;
            }
            else
            {
                maxPosition = endPosition.position.x;
                minPosition = startPosition.position.x;
            }
            float clampedX = Mathf.Clamp(transform.position.x, minPosition, maxPosition);

            transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);

            currentPositionAlongRail = clampedX;
        }
        if (axis == PistonRailAxis.Y)
        {
            if (startPosition.position.y > endPosition.position.y)
            {
                maxPosition = startPosition.position.y;
                minPosition = endPosition.position.y;
            }
            else
            {
                maxPosition = endPosition.position.y;
                minPosition = startPosition.position.y;
            }
            float clampedY = Mathf.Clamp(transform.position.y, minPosition, maxPosition);

            transform.position = new Vector3(transform.position.x, clampedY, transform.position.z);

            currentPositionAlongRail = clampedY;
        }
        if (axis == PistonRailAxis.Z)
        {
            if (startPosition.position.z > endPosition.position.z)
            {
                maxPosition = startPosition.position.z;
                minPosition = endPosition.position.z;
            }
            else
            {
                maxPosition = endPosition.position.z;
                minPosition = startPosition.position.z;
            }
            float clampedZ = Mathf.Clamp(transform.position.z, minPosition, maxPosition);

            transform.position = new Vector3(transform.position.x, transform.position.y, clampedZ);

            currentPositionAlongRail = clampedZ;
        }
    }

    private void ResetSpeedAtEdge()
    {
        float currentVelocity = 0f;
        if (axis == PistonRailAxis.X) currentVelocity = rb.linearVelocity.x;
        if (axis == PistonRailAxis.Y) currentVelocity = rb.linearVelocity.y;
        if (axis == PistonRailAxis.Z) currentVelocity = rb.linearVelocity.z;
        currentVelocity = Mathf.Clamp(currentVelocity, -0.1f, 0.1f);
        float nextPosition = currentPositionAlongRail + currentVelocity;

        if(nextPosition > maxPosition ||  nextPosition < minPosition)
        {
            rb.linearVelocity = Vector3.zero;
        }
    }
}
