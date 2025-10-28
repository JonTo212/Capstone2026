using System.Collections;
using UnityEngine;

public class UmbrellaProp : Prop
{
    [SerializeField] private float orientationSpeed;

    private void Awake()
    {
        Init();
    }

    private void FixedUpdate()
    {
        if (AttachedTransform != null) AttachedTransform.GetComponent<Rigidbody>().AddForce(-Physics.gravity, ForceMode.Force);
    }

    private void UpdateOrientation(Vector3 incomingForce)
    {
        if (incomingForce == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, incomingForce.normalized);
        Quaternion deltaRotation = targetRotation * Quaternion.Inverse(transform.rotation);

        deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f) angle -= 360f;

        float clampedTime = Mathf.Max(Time.fixedDeltaTime, 0.001f);
        Vector3 angularVelocity = axis * angle * Mathf.Deg2Rad / clampedTime;
        Vector3 angularError = angularVelocity - Rb.angularVelocity;

        Rb.AddTorque(angularError * orientationSpeed, ForceMode.Acceleration);
    }


    private void Reflect()
    {
        Vector3 accumulatedForce = Rb.GetAccumulatedForce();
        if (accumulatedForce == Vector3.zero) return;

        if (Vector3.Dot(Rb.GetAccumulatedForce(), transform.up) > 0f)
        {
            ApplyForceInDirection(accumulatedForce.normalized, accumulatedForce.magnitude, ForceMode.Force);
        }
        else
        {
            Vector3 reflected = Vector3.Reflect(Rb.GetAccumulatedForce().normalized, transform.up) * accumulatedForce.magnitude;
            PushNearbyObjects(reflected, Rb.position + transform.up * 0.5f);
        }
    }

    private void PushNearbyObjects(Vector3 reflectedForce, Vector3 origin)
    {
        Collider[] colliders = Physics.OverlapSphere(origin, 5f);
        foreach (var col in colliders)
        {
            Rigidbody colRb = col.attachedRigidbody;
            if (colRb != null && colRb != Rb)
            {
                colRb.AddForce(reflectedForce, ForceMode.Impulse);
            }
        }
    }

    public override void ApplyForceInDirection(Vector3 direction, float magnitude, ForceMode forceMode, Transform forceApplier = null)
    {
        base.ApplyForceInDirection(direction, magnitude, forceMode);
        AbsorbForce(direction);
    }

    private void AbsorbForce(Vector3 incomingForce)
    {
        if (AttachedTransform == null) return;
        float transferMultiplier = 0.5f;
        AttachedTransform.GetComponent<Rigidbody>().AddForce(incomingForce * transferMultiplier);
    }
}
