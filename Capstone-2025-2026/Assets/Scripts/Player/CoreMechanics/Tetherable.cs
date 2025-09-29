using UnityEngine;
using System.Collections.Generic;
[RequireComponent(typeof(Rigidbody))]
public class Tetherable : MonoBehaviour
{
    private Rigidbody rb;
    private float defaultDrag;
    private float defaultAngularDrag;
    public Vector3 forceBeingReceived;
    public List<Vector3> forcesBeingReceived;
    public Rigidbody Rb => rb;
    public Vector3 ForceBeingReceived => forceBeingReceived;

    public bool grappleAble = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        defaultDrag = rb.linearDamping;
        defaultAngularDrag = rb.angularDamping;
    }

    public void OnPickUp()
    {
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.angularVelocity = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
    }

    public void SetLinearDamping(float newDrag)
    {
        rb.linearDamping = newDrag;
    }

    public void OnRelease()
    {
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.linearDamping = defaultDrag;
        rb.angularDamping = defaultAngularDrag;
    }

    public void ApplyForceInDirection(Vector3 dir, float strength, ForceMode forceType)
    {
        rb.AddForce(dir * strength, forceType);
        forceBeingReceived = dir * strength;
    }
}
