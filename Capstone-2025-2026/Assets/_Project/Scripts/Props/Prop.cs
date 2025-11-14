using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody), typeof(Outline))]
public abstract class Prop : MonoBehaviour, ISnareable, IHoldable, ITetherable
{
    [SerializeField] protected bool debugThisProp = false;

    //protected means only derived classes can access these values
    public Rigidbody rb { get; protected set; }
    protected Lasso playerLasso;
    [SerializeField] protected List<JointTether> attachedTethers = new List<JointTether>();
    [SerializeField] protected List<Transform> connectedObject = new List<Transform>();
    [SerializeField] protected List<Transform> connectedAnchors = new List<Transform>();
    protected float defaultDrag;
    protected float defaultAngularDrag;

    //getters/setters - default value is false (protected set means only derived classes can change IsHeld)
    public virtual bool IsHeld { get; protected set; } = false;
    public virtual bool IsSnared { get; protected set; } = false;
    public virtual bool IsBeingPulled { get; set; } = false;
    public virtual bool isTetherPulled { get; protected set; } = false;

    public bool IsTouchingSurface {  get; protected set; } = false;

    public Rigidbody Rb => rb;
    public Transform AttachedTransform { get; set; }
    public Outline ObjectOutline { get; set; }
    [field: SerializeField] public List<Transform> GrabPoints { get; protected set; }
    [field: SerializeField] public int faceRows { get; protected set; }
    [field: SerializeField] public int faceColumns { get; protected set; }

    public event Action OnPropDestroyed;

    //virtual functions can be overridden by the derived classes
    //default behaviour is updating IsHeld and parenting the object to a given transform (i.e. player hand)

    //Gets the total force applied to this objct. NOTE: Should only be read in Update or the value will  be incorrect
    public Vector3 totalForceApplied { get; protected set; } = Vector3.zero;

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
            foreach (Transform t in GrabPoints)
            {
                t.gameObject.SetActive(false);
            }
        }
    }

    protected virtual void Update()
    {
        HandleOutlineColors();
    }

    protected virtual void OnDestroy()
    {
        OnPropDestroyed?.Invoke();
    }

    protected virtual void FixedUpdate()
    {
        totalForceApplied = Vector3.zero;
        totalForceApplied += GetForcesFromJoint();
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
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.constraints = RigidbodyConstraints.None;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.linearDamping = defaultDrag;
        rb.angularDamping = defaultAngularDrag;
        AttachedTransform = null;

        if(transform != null) transform.SetParent(null);
    }

    public virtual void OnAttachTether()
    {

    }

    public virtual void OnTetherPull(JointTether tether, Transform targetAnchorTransform, Transform targetObjectTransform)
    {
        isTetherPulled = true;
        attachedTethers.Add(tether);
        connectedObject.Add(targetObjectTransform);
        connectedAnchors.Add(targetAnchorTransform);
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public virtual void OnDetachTether(JointTether tether, Transform targetAnchorTransform, Transform targetObjectTransform)
    {
        if (attachedTethers.IndexOf(tether) < 0) return;
        connectedObject.RemoveAt(attachedTethers.IndexOf(tether));
        connectedAnchors.RemoveAt(attachedTethers.IndexOf((tether)));
        attachedTethers.Remove(tether);
        if (attachedTethers.Count <= 0)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            isTetherPulled = false;
            ObjectOutline.enabled = false;
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

    private void HandleOutlineColors()
    {
        if (IsSnared || attachedTethers.Count > 0)
        {
            ActivateOutline(true);
            if (attachedTethers.Count > 0)
            {
                foreach (JointTether jointTether in attachedTethers)
                {
                    if (jointTether.isActivated)
                    {
                        if (ObjectOutline.outlineState != Outline.OutlineStates.TetherActive)
                            ObjectOutline.TetherActiveColor(); return;
                    }
                }
                if (ObjectOutline.outlineState != Outline.OutlineStates.TetherInnactive)
                    ObjectOutline.TetherInactiveColor(); return;
            }
            if (IsSnared)
            {
                if (ObjectOutline.outlineState != Outline.OutlineStates.Snare)
                {
                    ObjectOutline.SnareColor(); return;
                }
            }
        }
    }

    #endregion

    #region IHoldable
    public virtual void OnHold(Transform newParent)
    {
        IsHeld = true;
        IsSnared = false;
        IsBeingPulled = false;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.constraints = RigidbodyConstraints.FreezePosition;
        transform.SetParent(newParent);
        transform.position = newParent.position;
        ActivateOutline(false);
    }

    public virtual void OnThrow(Vector3 dir, float magnitude)
    {
        OnRelease();
        ApplyForceInDirection(dir, magnitude, ForceMode.Impulse);
    }

    #endregion


    #region Force Addition

    public virtual void ApplyForceInDirection(Vector3 direction, float magnitude, ForceMode forceMode, Transform forceApplier = null)
    {
        Rb.AddForce(direction * magnitude, forceMode);

        if(playerLasso == null && forceApplier != null && forceApplier.GetComponent<Lasso>() != null)
        {
            totalForceApplied += direction * magnitude;
        }
    }

    protected Vector3 GetForcesFromJoint()
    {
        Vector3 totalForce = Vector3.zero;

        for (int i = 0; i < attachedTethers.Count; i++)
        {
            totalForce += attachedTethers[i].GetCurrentForce(rb);
        }

        return totalForce;
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

    #region Utility
    private void OnCollisionEnter(Collision collision)
    {
        IsTouchingSurface = true;
    }

    private void OnCollisionStay(Collision collision)
    {
        IsTouchingSurface = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        IsTouchingSurface = false;
    }

    protected void PropDebug(object message) { if (debugThisProp == true) Debug.Log(message); }

    protected void PropWarning(object message) { if (debugThisProp == true) Debug.LogWarning(message); }

    protected void PropError(object message) { if (debugThisProp == true) Debug.LogError(message); }
    #endregion
}