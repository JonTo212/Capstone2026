using System.Collections.Generic;
using UnityEngine;

public enum RailAxis
{
    X,
    Y,
    Z
}

[RequireComponent(typeof(BoxCollider))]
public class Rail : MonoBehaviour
{
    [SerializeField] private BoxCollider railCol;
    private RailSystemHandler systemHandler;
    public bool Attached { get; private set; }

    private void Awake()
    {
        railCol = GetComponent<BoxCollider>();
        systemHandler = GetComponentInParent<RailSystemHandler>();
    }

    public RailAxis GetRailDir()
    {
        Vector3 adjustedScale = Vector3.Scale(railCol.size, transform.localScale);

        float x = Mathf.Abs(adjustedScale.x);
        float y = Mathf.Abs(adjustedScale.y);
        float z = Mathf.Abs(adjustedScale.z);

        float largest = Mathf.Max(x, Mathf.Max(y, z));

        if (largest == x) return RailAxis.X;
        if (largest == y) return RailAxis.Y;
        return RailAxis.Z;
    }

    public Vector3 GetLocalSnapPosition(Vector3 worldPos)
    {
        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        Vector3 center = railCol.center;
        RailAxis axis = GetRailDir();

        switch (axis)
        {
            case RailAxis.X: localPos.y = center.y; localPos.z = center.z; break;
            case RailAxis.Y: localPos.x = center.x; localPos.z = center.z; break;
            case RailAxis.Z: localPos.x = center.x; localPos.y = center.y; break;
        }

        return transform.TransformPoint(localPos);
    }

    public Vector3 GetWorldDirection()
    {
        RailAxis axis = GetRailDir();
        switch (axis)
        {
            case RailAxis.X: return transform.right;
            case RailAxis.Y: return transform.up;
            default: return transform.forward;
        }
    }

    public float GetDistanceFromCenterLine(Vector3 worldPos)
    {
        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        Vector3 center = railCol.center;
        RailAxis axis = GetRailDir();

        Vector3 projected;

        switch (axis)
        {
            case RailAxis.X:
                projected = new Vector3(localPos.x, center.y, center.z);
                break;

            case RailAxis.Y:
                projected = new Vector3(center.x, localPos.y, center.z);
                break;

            case RailAxis.Z:
                projected = new Vector3(center.x, center.y, localPos.z);
                break;

            default:
                projected = center;
                break;
        }

        return Vector3.Distance(localPos, projected);
    }

    public Vector3 ClampToRailBounds(Vector3 worldPos)
    {
        Vector3 local = transform.InverseTransformPoint(worldPos);
        Vector3 halfSize = railCol.size * 0.5f;

        RailAxis axis = GetRailDir();

        switch (axis)
        {
            case RailAxis.X:
                local.x = Mathf.Clamp(local.x, -halfSize.x, halfSize.x);
                break;

            case RailAxis.Y:
                local.y = Mathf.Clamp(local.y, -halfSize.y, halfSize.y);
                break;

            case RailAxis.Z:
                local.z = Mathf.Clamp(local.z, -halfSize.z, halfSize.z);
                break;
        }

        return transform.TransformPoint(local);
    }

    public void EnforceRailBounds(Prop prop)
    {
        // 1. Convert Position and Velocity to Local Space
        Vector3 localPos = transform.InverseTransformPoint(prop.transform.position);
        Vector3 localVel = transform.InverseTransformDirection(prop.Rb.linearVelocity);

        Vector3 halfSize = railCol.size * 0.5f;
        RailAxis axis = GetRailDir();
        bool hitWall = false;

        // 2. Check bounds based on axis
        switch (axis)
        {
            case RailAxis.X:
                // Check Positive Bound
                if (localPos.x > halfSize.x)
                {
                    localPos.x = halfSize.x;     // Hard Snap Position
                    if (localVel.x > 0) localVel.x = 0; // Kill Outward Velocity
                    hitWall = true;
                }
                // Check Negative Bound
                else if (localPos.x < -halfSize.x)
                {
                    localPos.x = -halfSize.x;
                    if (localVel.x < 0) localVel.x = 0;
                    hitWall = true;
                }
                break;

            case RailAxis.Y:
                if (localPos.y > halfSize.y)
                {
                    localPos.y = halfSize.y;
                    if (localVel.y > 0) localVel.y = 0;
                    hitWall = true;
                }
                else if (localPos.y < -halfSize.y)
                {
                    localPos.y = -halfSize.y;
                    if (localVel.y < 0) localVel.y = 0;
                    hitWall = true;
                }
                break;

            case RailAxis.Z:
                if (localPos.z > halfSize.z)
                {
                    localPos.z = halfSize.z;
                    if (localVel.z > 0) localVel.z = 0;
                    hitWall = true;
                }
                else if (localPos.z < -halfSize.z)
                {
                    localPos.z = -halfSize.z;
                    if (localVel.z < 0) localVel.z = 0;
                    hitWall = true;
                }
                break;
        }

        // 3. Apply changes back to Rigidbody only if we hit the wall
        if (hitWall)
        {
            prop.Rb.position = transform.TransformPoint(localPos);
            prop.Rb.linearVelocity = transform.TransformDirection(localVel);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Prop prop))
        {
            systemHandler.OnRailEnter(this, prop);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out Prop prop))
        {
            systemHandler.OnRailExit(this, prop);
        }
    }

    private void OnDrawGizmos()
    {
        var dir = Vector3.Scale(GetWorldDirection(), railCol.bounds.size / 2f); //todo: fix rotation

        Gizmos.color = GetRailDir() switch
        {
            RailAxis.X => Color.red,
            RailAxis.Y => Color.green,
            RailAxis.Z => Color.blue,
            _ => Color.white //default
        };

        Gizmos.DrawRay(transform.position - dir, dir * 2f);
    }
}
