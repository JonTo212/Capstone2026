using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;

[RequireComponent(typeof(Rigidbody), typeof(Outline))]
public abstract class Prop : MonoBehaviour, ISnareable, IHoldable, ITetherable
{
    [SerializeField] protected bool debugThisProp = false;
    [SerializeField] protected float coyoteFallDelay = 0.3f;

    //protected means only derived classes can access these values
    [SerializeField] protected List<JointTether> attachedTethers = new List<JointTether>();
    [SerializeField] protected List<Transform> connectedObject = new List<Transform>();
    [SerializeField] protected List<Transform> connectedAnchors = new List<Transform>();

    //getters/setters - default value is false (protected set means only derived classes can change IsHeld)
    public virtual bool IsHeld { get; protected set; } = false;
    public virtual bool IsSnared { get; protected set; } = false;
    public virtual bool IsBeingPulled { get; set; } = false;
    public virtual bool IsTetherPulled { get; protected set; } = false;
    public virtual bool IsTouchingSurface {  get; protected set; } = false;

    private bool didFixedUpdateRun = true;
    protected Lasso lassoRef;

    public Rigidbody Rb { get; protected set; }
    public Transform AttachedTransform { get; set; }
    public Outline ObjectOutline { get; set; }
    [field: SerializeField] public List<Transform> GrabPoints { get; protected set; }
    [field: SerializeField] public int FaceRows { get; protected set; }
    [field: SerializeField] public int FaceColumns { get; protected set; }

    public event Action OnPropDestroyed;

    //virtual functions can be overridden by the derived classes
    //default behaviour is updating IsHeld and parenting the object to a given transform (i.e. player hand)

    //Gets the total force applied to this objct. NOTE: Should only be read in Update or the value will  be incorrect
    public Vector3 totalForceApplied { get; protected set; } = Vector3.zero;
    private Vector3 storedTotalForce = Vector3.zero;

    protected virtual void Init()
    {
        Rb = GetComponent<Rigidbody>();

        ObjectOutline = GetComponent<Outline>();
        ObjectOutline.OutlineColor = Color.green;
        ObjectOutline.OutlineWidth = 3f;
        ObjectOutline.enabled = false;

        var generator = GetComponent<IGrabPointGenerator>();
        if(generator != null)
        {
            Collider col = GetComponent<Collider>();
            GrabPoints = generator.GeneratePoints(col, FaceRows, FaceColumns);
            foreach (Transform t in GrabPoints)
            {
                t.gameObject.SetActive(false);
            }
        }
    }

    protected virtual void Update()
    {
        HandleOutlineColors();
        HandleForceAppliedVariable();
    }

    protected virtual void LateUpdate()
    {
        
    }

    protected virtual void OnDestroy()
    {
        OnPropDestroyed?.Invoke();
    }

    protected virtual void FixedUpdate()
    {
        storedTotalForce += GetForcesFromJoint();
        didFixedUpdateRun = true;
    }

    #region ISnareable
    public virtual void OnSnare()
    {
        IsSnared = true;
        IsHeld = false;
        IsBeingPulled = false;
        Rb.interpolation = RigidbodyInterpolation.Interpolate;
        Rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        Rb.angularVelocity = Vector3.zero;
        Rb.linearVelocity = Vector3.zero;
    }

    public virtual void OnRelease()
    {
        IsSnared = false;
        IsHeld = false;
        IsBeingPulled = false;
        Rb.interpolation = RigidbodyInterpolation.None;
        Rb.constraints = RigidbodyConstraints.None;
        Rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        AttachedTransform = null;

        Invoke(nameof(CoyoteFall), coyoteFallDelay);

        if(transform != null) transform.SetParent(null);
    }

    public void SetLassoRef(Lasso lasso)
    {
        lassoRef = lasso;
    }

    public virtual void OnAttachTether()
    {

    }

    public virtual void OnTetherPull(JointTether tether, Transform targetAnchorTransform, Transform targetObjectTransform)
    {
        IsTetherPulled = true;
        attachedTethers.Add(tether);
        connectedObject.Add(targetObjectTransform);
        connectedAnchors.Add(targetAnchorTransform);
        Rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public virtual void OnDetachTether(JointTether tether, Transform targetAnchorTransform, Transform targetObjectTransform)
    {
        if (attachedTethers.IndexOf(tether) < 0) return;
        connectedObject.RemoveAt(attachedTethers.IndexOf(tether));
        connectedAnchors.RemoveAt(attachedTethers.IndexOf((tether)));
        attachedTethers.Remove(tether);
        if (attachedTethers.Count <= 0)
        {
            Rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            IsTetherPulled = false;
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
        Rb.interpolation = RigidbodyInterpolation.None;
        Rb.constraints = RigidbodyConstraints.FreezePosition;
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

        if(forceApplier != null && forceApplier.GetComponent<Lasso>() != null)
        {
            storedTotalForce += direction * magnitude;
        }
    }

    protected Vector3 GetForcesFromJoint()
    {
        Vector3 totalForce = Vector3.zero;

        for (int i = 0; i < attachedTethers.Count; i++)
        {
            totalForce += attachedTethers[i].GetCurrentForce(Rb);
        }

        return totalForce;
    }

    protected void HandleForceAppliedVariable()
    {
        if (storedTotalForce.sqrMagnitude > 0 || didFixedUpdateRun)
        {
            totalForceApplied = storedTotalForce;
        }
        storedTotalForce = Vector3.zero;

        didFixedUpdateRun = false;
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

    private void CoyoteFall()
    {
        Rb.useGravity = true;
    }

    protected void PropDebug(object message) { if (debugThisProp == true) Debug.Log(message); }

    protected void PropWarning(object message) { if (debugThisProp == true) Debug.LogWarning(message); }

    protected void PropError(object message) { if (debugThisProp == true) Debug.LogError(message); }
    #endregion
}