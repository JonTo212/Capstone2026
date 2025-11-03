using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody), typeof(Outline))]
public abstract class Prop : MonoBehaviour, ISnareable, IHoldable, ITetherable
{
    //protected means only derived classes can access these values
    protected Rigidbody rb;
    protected List<GameObject> attachedTethers = new List<GameObject>();
    protected float defaultDrag;
    protected float defaultAngularDrag;

    //getters/setters - default value is false (protected set means only derived classes can change IsHeld)
    public virtual bool IsHeld { get; protected set; } = false;
    public virtual bool IsSnared { get; protected set; } = false;
    public virtual bool IsBeingPulled { get; set; } = false;
    public virtual bool isTetherPulled { get; protected set; } = false;
    public Rigidbody Rb => rb;
    public Transform AttachedTransform { get; set; }
    public Outline ObjectOutline { get; set; }
    [field: SerializeField] public List<Transform> GrabPoints { get; protected set; }
    [field: SerializeField] public int faceRows { get; protected set; }
    [field: SerializeField] public int faceColumns { get; protected set; }

    public event Action OnPropDestroyed;

    //virtual functions can be overridden by the derived classes
    //default behaviour is updating IsHeld and parenting the object to a given transform (i.e. player hand)

    protected virtual void Init()
    {
        rb = GetComponent<Rigidbody>();
        defaultDrag = rb.linearDamping;
        defaultAngularDrag = rb.angularDamping;

        ObjectOutline = GetComponent<Outline>();
        ObjectOutline.OutlineColor = Color.green;
        ObjectOutline.OutlineWidth = 3f;
        ObjectOutline.enabled = false;

        var generator = GetComponent<IGrabPointGenerator>();
        if(generator != null)
        {
            Collider col = GetComponent<Collider>();
            GrabPoints = generator.GeneratePoints(col, faceRows, faceColumns);
            foreach(Transform t in GrabPoints)
            {
                t.gameObject.SetActive(false);
            }
        }
    }

    protected virtual void OnDestroy()
    {
        OnPropDestroyed?.Invoke();
    }

    #region ISnareable
    public virtual void OnSnare()
    {
        IsSnared = true;
        IsHeld = false;
        IsBeingPulled = false;
        //rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.angularVelocity = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
        //rb.linearDamping = 25f;
        //rb.angularDamping = 25f;
    }

    public virtual void OnRelease()
    {
        IsSnared = false;
        IsHeld = false;
        IsBeingPulled = false;
        rb.useGravity = false;
        Invoke(nameof(GravDelay), 0.3f);
        rb.interpolation = RigidbodyInterpolation.None;
        rb.constraints = RigidbodyConstraints.None;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.linearDamping = defaultDrag;
        rb.angularDamping = defaultAngularDrag;
        AttachedTransform = null;

        if(transform != null) transform.SetParent(null);
    }

    public void GravDelay()
    {
        rb.useGravity = true;
    }
    public virtual void OnAttachTether()
    {

    }

    public virtual void OnTetherPull(GameObject tether)
    {
        isTetherPulled = true;
        attachedTethers.Add(tether);
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public virtual void OnDetachTether(GameObject tether)
    {
        attachedTethers.Remove(tether);
        if (attachedTethers.Count <= 0)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            isTetherPulled = false;
        }
    }
    public virtual void ActivateOutline(bool activate)
    {
        ObjectOutline.enabled = activate;
    }

    public virtual void SetOutlineColour(Color newColour)
    {
        ObjectOutline.OutlineColor = newColour;
    }

    public virtual void SetOutlineWidth(float newWidth)
    {
        ObjectOutline.OutlineWidth = newWidth;
    }

    #endregion

    #region IHoldable
    public virtual void OnHold(Transform newParent)
    {
        IsHeld = true;
        IsSnared = false;
        IsBeingPulled = false;
        //rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.constraints = RigidbodyConstraints.FreezePosition;
        transform.SetParent(newParent);
        transform.position = newParent.position;
        ActivateOutline(false);
    }

    public virtual void OnThrow(Vector3 dir, float magnitude)
    {
        OnRelease();
        rb.isKinematic = false;
        ApplyForceInDirection(dir, magnitude, ForceMode.Impulse);
    }

    #endregion

    #region Force Addition

    public virtual void ApplyForceInDirection(Vector3 direction, float magnitude, ForceMode forceMode, Transform forceApplier = null)
    {
        Rb.AddForce(direction * magnitude, forceMode);
    }

    #endregion

    #region Grab Points

    public virtual Transform CheckNearestGrabPoint(Vector3 grabPos)
    {
        Transform nearestGrabPoint = null;
        float currentNearestDist = float.MaxValue;

        foreach (var point in GrabPoints)
        {
            float dist = Vector3.Distance(grabPos, point.position);

            if (dist < currentNearestDist)
            {
                currentNearestDist = dist;
                nearestGrabPoint = point;
            }
        }

        return nearestGrabPoint;
    }

    public void EnableAllGrabPoints(bool active)
    {
        foreach (var point in GrabPoints)
        {
            point.gameObject.SetActive(active);
        }
    }

    #endregion
}