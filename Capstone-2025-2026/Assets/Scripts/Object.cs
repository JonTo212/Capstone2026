using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(Rigidbody))]
public abstract class Object : MonoBehaviour
{
    //serialized variables
    [field: SerializeField] public PhysicsDictionary.MaterialType MaterialType { get; protected set; }
    [field: SerializeField] public float GravityMultiplier { get; protected set; }

    //protected components
    public Rigidbody Rb { get; protected set; }
    protected Collider col;

    //protected variables
    private bool useGravity = true;

    #region Setup
    protected virtual void Init()
    {
        Rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        col.material = PhysicsDictionary.frictionMaterial[MaterialType];
    }

    protected virtual void FixedUpdate()
    {
        ApplyGravity();
    }
    #endregion

    #region Apply Force Function
    public void ApplyForce(Vector3 direction, float magnitude, ForceMode forceType)
    {
        Rb.AddForce(direction * magnitude, forceType);
    }
    #endregion

    #region Gravity
    public void EnableGravity()
    {
        useGravity = true;
    }

    public void DisableGravity()
    {
        useGravity = false;
    }

    protected void ApplyGravity()
    {
        if(useGravity) ApplyForce(Vector3.down, -Physics.gravity.y * GravityMultiplier, ForceMode.Acceleration);
    }
    #endregion
}
