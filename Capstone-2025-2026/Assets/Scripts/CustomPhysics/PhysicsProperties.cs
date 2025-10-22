using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Physics/Physics Properties")]
public class PhysicsProperties : ScriptableObject
{
    public int SimulationPriority = 0;
    public bool UseGravity = true;
    public bool IsKinematic = false;
    public bool FreezeRotation = false;
    public bool FreezePosition = false;
    public float GravityMultiplier = 1f;
    public float Mass = 1f;
    public float MassScale = 1f;
    [Range(0f, 1f)] public float KineticFriction = 0.5f;
    [Range(0f, 1f)] public float StaticFriction = 0.5f;
    public float LinearDrag = 0f;
    public float AngularDrag = 0.05f;
    public float MaxVelocity;
    public float MaxVelocityClampStrength;
    public PhysicsMaterialCombine FrictionType = PhysicsMaterialCombine.Average;
    public RigidbodyConstraints RigidbodyConstraints = RigidbodyConstraints.None;
}

[Serializable]
public struct PhysicsPropertiesInstance
{
    public int SimulationPriority;
    public bool UseGravity;
    public bool IsKinematic;
    public bool FreezeRotation;
    public bool FreezePosition;
    public float GravityMultiplier;
    public float Mass;
    public float MassScale;
    public float KineticFriction;
    public float StaticFriction;
    public float LinearDrag;
    public float AngularDrag;
    public float MaxVelocity;
    public float MaxVelocityClampStrength;
    public PhysicsMaterialCombine FrictionType;
    public RigidbodyConstraints RigidbodyConstraints;
}
