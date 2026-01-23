using UnityEngine;

public class Rail : MonoBehaviour
{
    public enum RailAxis
    {
        X,
        Y,
        Z
    }

    public Prop movableObjRef;
    private BoxCollider railCol;
    public bool Attached { get; private set; }

    private void Awake()
    {
        railCol = GetComponent<BoxCollider>();
    }

    private void FixedUpdate()
    {
        if (Attached) ConstrainToAxis();
    }

    public RailAxis GetRailDir()
    {
        Vector3 v = railCol.size;
        float largest = Mathf.Max(v.x, Mathf.Max(v.y, v.z));

        if (largest == v.x) return RailAxis.X;
        if (largest == v.y) return RailAxis.Y;
        return RailAxis.Z;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Prop prop))
        { 
            if(prop == movableObjRef)
            {
                Attached = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out Prop prop))
        {
            if (prop == movableObjRef)
            {
                Attached = false;
            }
        }
    }

    public void SetAttachedObject(Prop movableObj)
    {
        movableObjRef = movableObj;
    }

    public void ConstrainToAxis()
    {
        Vector3 railAxis = Vector3.zero;
        switch (GetRailDir())
        {
            case RailAxis.X: railAxis = transform.right; break;
            case RailAxis.Y: railAxis = transform.up; break;
            case RailAxis.Z: railAxis = transform.forward; break;
        }

        Vector3 propVel = movableObjRef.Rb.linearVelocity;
        Vector3 velOnRail = Vector3.Project(propVel, railAxis);

        movableObjRef.Rb.linearVelocity = velOnRail;
    }
}
