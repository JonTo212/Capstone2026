using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Tetherable : MonoBehaviour
{
    private Rigidbody rb;
    private float defaultDrag;
    private float defaultAngularDrag;
    public Rigidbody Rb => rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        defaultDrag = rb.linearDamping;
        defaultAngularDrag = rb.angularDamping;
    }

    public void OnPickUp()
    {
        rb.useGravity = false;
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
        rb.linearDamping = defaultDrag;
        rb.angularDamping = defaultAngularDrag;
    }

    public void ApplyForceInDirection(Vector3 dir, float strength, ForceMode forceType)
    {
        rb.AddForce(dir * strength, forceType);
    }
}
