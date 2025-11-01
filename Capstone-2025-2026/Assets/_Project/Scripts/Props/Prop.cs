using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody), typeof(Outline))]
public abstract class Prop : MonoBehaviour, ISnareable, IHoldable, ITetherable
{
    [SerializeField] protected bool objectDebug = false;

    //protected means only derived classes can access these values
    protected Rigidbody rb;
    protected Lasso playerLasso;
    protected List<GameObject> attachedTethers = new List<GameObject>();
    [SerializeField] protected List <Transform> connectedTransform = new List<Transform>();
    [SerializeField] protected List<ConfigurableJoint> tetherJoints = new List<ConfigurableJoint>(); 
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

    public event Action OnPropDestroyed;

    //virtual functions can be overridden by the derived classes
    //default behaviour is updating IsHeld and parenting the object to a given transform (i.e. player hand)

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
    }

    protected virtual void Update()
    {

    }

    protected virtual void FixedUpdate()
    {
        //totalForceApplied += GetForcesFromJoint();

        if(objectDebug == true)
        {
            //Debug.Log(totalForceApplied);
        }
    }

    protected virtual void LateUpdate()
    {
        GetForcesFromJoint();

        totalForceApplied = Vector3.zero;
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

    public virtual void OnTetherPull(GameObject tether, Transform target, ConfigurableJoint joint)
    {
        isTetherPulled = true;
        tetherJoints.Add(joint);
        attachedTethers.Add(tether);
        connectedTransform.Add(target);
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public virtual void OnDetachTether(GameObject tether, Transform target, ConfigurableJoint joint)
    {
        tetherJoints.RemoveAt(attachedTethers.IndexOf(tether));
        connectedTransform.RemoveAt(attachedTethers.IndexOf(tether));
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
        rb.isKinematic = true;
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

        if(playerLasso == null && forceApplier.GetComponent<Lasso>() != null)
        {
            totalForceApplied += direction * magnitude;
        }
    }

    #endregion

    protected Vector3 GetForcesFromJoint()
    {
        Vector3 totalForce = Vector3.zero;

        for (int i = 0; i < attachedTethers.Count; i++)
        {
            ConfigurableJoint joint = tetherJoints[i];

            Vector3 worldAnchor = transform.TransformPoint(joint.anchor);
            if (objectDebug) Debug.Log(worldAnchor);
            Vector3 worldTargetAnchor = connectedTransform[i].TransformPoint(joint.anchor);
            Vector3 difference = worldTargetAnchor - worldAnchor;

            float springConstant = joint.xDrive.positionSpring;
            float dampener = joint.xDrive.positionDamper;

            Vector3 forceFromJoint = (springConstant * difference - dampener * rb.linearVelocity) * Time.fixedDeltaTime;

            totalForce += forceFromJoint;
        }

        //if(objectDebug) Debug.Log(totalForce);

        return totalForce;
    }
}