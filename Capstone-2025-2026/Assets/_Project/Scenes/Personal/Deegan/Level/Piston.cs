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
    public Transform startPosition;
    public Transform endPosition;
    public PistonRailAxis axis = PistonRailAxis.X;
    private Rigidbody rb;
    public float maxPosition = 0f;
    public float minPosition = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (axis == PistonRailAxis.X) rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ;
        if (axis == PistonRailAxis.Y) rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
        if (axis == PistonRailAxis.Z) rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY;
    }

    // Update is called once per frame
    void Update()
    {

        if(axis == PistonRailAxis.X)
        {
            if(startPosition.position.x >  endPosition.position.x)
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
        }
    }
}
