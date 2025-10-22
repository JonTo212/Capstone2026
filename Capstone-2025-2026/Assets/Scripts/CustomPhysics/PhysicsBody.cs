#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
#endif
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PhysicsBody : MonoBehaviour
{
    [SerializeField] private PhysicsProperties _physicsSO;
    [SerializeField] private bool _useSO = true;
    [SerializeField] private PhysicsPropertiesInstance _localOverrides;

    private PhysicsMaterial _physicsMat;
    private Collider _col;
    private int _lastPriority;
    private float _overshootForce = 250f;
    private float _tick => TickManager.TickInterval;

    public bool IsActive { get; private set; }
    public Rigidbody Rb { get; private set; }
    public Vector3 TotalForceThisFrame { get; private set; }

    public event Action OnPriorityChanged;

    public PhysicsPropertiesInstance CurrentProperties
    {
        get
        {
            if (_useSO && _physicsSO != null)
            {
                return new PhysicsPropertiesInstance
                {
                    SimulationPriority = _physicsSO.SimulationPriority,
                    UseGravity = _physicsSO.UseGravity,
                    IsKinematic = _physicsSO.IsKinematic,
                    FreezeRotation = _physicsSO.FreezeRotation,
                    FreezePosition = _physicsSO.FreezePosition,
                    GravityMultiplier = _physicsSO.GravityMultiplier,
                    Mass = _physicsSO.Mass,
                    MassScale = _physicsSO.MassScale,
                    KineticFriction = _physicsSO.KineticFriction,
                    StaticFriction = _physicsSO.StaticFriction,
                    LinearDrag = _physicsSO.LinearDrag,
                    AngularDrag = _physicsSO.AngularDrag,
                    MaxVelocity = _physicsSO.MaxVelocity,
                    MaxVelocityClampStrength = _physicsSO.MaxVelocityClampStrength,
                    FrictionType = _physicsSO.FrictionType,
                    RigidbodyConstraints = _physicsSO.RigidbodyConstraints
                };
            }
            return _localOverrides;
        }
    }

    #if UNITY_EDITOR
    private void OnValidate()
    {
        int current = CurrentProperties.SimulationPriority;
        if (current != _lastPriority)
        {
            _lastPriority = current;
            OnPriorityChanged?.Invoke();
        }
    }
    #endif

    public void Init()
    {
        Rb = GetComponent<Rigidbody>();
        ApplyPropertiesToRigidbody();
        ApplyMaterialToCollider();

        TickManager.OnTick += OnPhysicsUpdate;
    }

    private void ApplyPropertiesToRigidbody()
    {
        Rb.mass = CurrentProperties.Mass;
        Rb.linearDamping = CurrentProperties.LinearDrag;
        Rb.angularDamping = CurrentProperties.AngularDrag;
        Rb.useGravity = false;
        Rb.isKinematic = CurrentProperties.IsKinematic;
        Rb.constraints = CurrentProperties.RigidbodyConstraints;
    }

    private void ApplyMaterialToCollider()
    {
        _col = GetComponents<Collider>().FirstOrDefault(c => !c.isTrigger);
        if (_col == null) return;

        _physicsMat = new PhysicsMaterial
        {
            dynamicFriction = CurrentProperties.KineticFriction,
            staticFriction = CurrentProperties.StaticFriction,
            frictionCombine = CurrentProperties.FrictionType
        };
        _col.material = _physicsMat;
    }

    private void OnDestroy()
    {
        TickManager.OnTick -= OnPhysicsUpdate;
    }

    public void OnPhysicsUpdate()
    {
        Rb.AddForce(TotalForceThisFrame, ForceMode.Acceleration);
        TotalForceThisFrame = Vector3.zero;

        ApplyVelocityClamp();

        //gravity is used as a baseline for the next frame
        if (CurrentProperties.UseGravity)
        {
            TotalForceThisFrame += Vector3.down * GetGravity();
        }
    }

    public void OnPostPhysicsUpdate()
    {
        //handle stuff like visuals here
    }

    public void ApplyVelocityClamp()
    {
        if (CurrentProperties.MaxVelocity <= 0f) return;

        Vector3 targetVelocity = Rb.linearVelocity.normalized * CurrentProperties.MaxVelocity;
        Vector3 velocityError = targetVelocity - Rb.linearVelocity;
        Vector3 springForce = _overshootForce * velocityError;
        float damping = 2f * Mathf.Sqrt(_overshootForce * Rb.mass); //critical damping = 2 * sqrt(springRate * mass)
        Vector3 dampingForce = -Rb.linearVelocity * damping;

        Vector3 totalForce = springForce + dampingForce;
        Rb.AddForce(totalForce, ForceMode.Acceleration);

        /*if (Rb.linearVelocity.magnitude > CurrentProperties.MaxVelocity)
        {
            float ratio = (Rb.linearVelocity.magnitude - CurrentProperties.MaxVelocity) / CurrentProperties.MaxVelocity;
            Vector3 softClampForce = -Rb.linearVelocity.normalized * ratio * CurrentProperties.MaxVelocityClampStrength;
            Rb.AddForce(softClampForce, ForceMode.Acceleration);

            if (Rb.linearVelocity.magnitude > CurrentProperties.MaxVelocity * 1.5f)
            {
                Rb.linearVelocity = Rb.linearVelocity.normalized * CurrentProperties.MaxVelocity;
            }
        }*/
    }

    public void ApplyExplosionForce(Vector3 origin, float radius, float magnitude, bool useMassScale = false)
    {
        Vector3 explosionDir = Rb.position - origin;
        float distance = explosionDir.magnitude;
        Vector3 direction = explosionDir.normalized;

        if (distance > radius || distance == 0f) return;
        float falloff = 1f - (distance / radius);

        Vector3 explosionForce = Vector3.zero;
        if (useMassScale) explosionForce = direction * magnitude * falloff / CurrentProperties.Mass * CurrentProperties.MassScale / _tick;
        else explosionForce = direction * magnitude * falloff / CurrentProperties.Mass / _tick;

        TotalForceThisFrame += explosionForce;
    }

    public void ApplyAccelerationForce(Vector3 direction, float magnitude)
    {
        TotalForceThisFrame += direction * magnitude;
    }

    public void ApplyMassDependentForce(Vector3 direction, float magnitude, bool useMassScale = false)
    {
        if(useMassScale) TotalForceThisFrame += direction * magnitude / CurrentProperties.Mass * CurrentProperties.MassScale;
        else TotalForceThisFrame += direction * magnitude / CurrentProperties.Mass;
    }

    public void ApplyImpulseForce(Vector3 direction, float magnitude, bool useMassScale = false)
    {
        if(useMassScale) TotalForceThisFrame += direction * magnitude / CurrentProperties.Mass * CurrentProperties.MassScale / _tick;
        else TotalForceThisFrame += direction * magnitude / CurrentProperties.Mass / _tick;
    }

    public void ApplyVelocityChange(Vector3 direction, float magnitude)
    {
        TotalForceThisFrame += direction * magnitude / _tick;
    }

    public void ApplyRotation(Vector3 additionalRot)
    {
        Rb.MoveRotation(Rb.rotation * Quaternion.Euler(additionalRot));
    }

    private float GetGravity()
    {
        return Mathf.Abs(Physics.gravity.y) * CurrentProperties.GravityMultiplier;
    }

    public Vector3 GetVelocity() => Rb.linearVelocity;
}

#if UNITY_EDITOR
[CustomEditor(typeof(PhysicsBody))]
public class PhysicsBodyEditor : Editor
{
    SerializedProperty scriptableObjectProperties;
    SerializedProperty overrideProperties;

    SerializedProperty overrideUseGravity;
    SerializedProperty overrideGravityMultiplier;
    SerializedProperty overrideMass;
    SerializedProperty overrideMassScale;
    SerializedProperty overrideLinearDrag;
    SerializedProperty overrideAngularDrag;
    SerializedProperty overrideKineticFriction;
    SerializedProperty overrideStaticFriction;
    SerializedProperty overrideMaxVelocity;
    SerializedProperty overrideMaxVelocityClampStrength;
    SerializedProperty overrideFrictionType;

    void OnEnable()
    {
        var physicsBody = target as PhysicsBody;
        if (physicsBody != null && physicsBody.GetComponent<Rigidbody>() != null)
        {
            physicsBody.GetComponent<Rigidbody>().hideFlags = HideFlags.HideInInspector;
        }

        scriptableObjectProperties = serializedObject.FindProperty("_physicsPropertiesScriptableObject");
        overrideProperties = serializedObject.FindProperty("_useScriptableObjectValues");

        overrideUseGravity = serializedObject.FindProperty("_useGravity");
        overrideGravityMultiplier = serializedObject.FindProperty("_gravityMultiplier");
        overrideMass = serializedObject.FindProperty("_mass");
        overrideMassScale = serializedObject.FindProperty("_massScale");
        overrideLinearDrag = serializedObject.FindProperty("_linearDrag");
        overrideAngularDrag = serializedObject.FindProperty("_angularDrag");
        overrideKineticFriction = serializedObject.FindProperty("_kineticFriction");
        overrideStaticFriction = serializedObject.FindProperty("_staticFriction");
        overrideMaxVelocity = serializedObject.FindProperty("_maxVelocity");
        overrideMaxVelocityClampStrength = serializedObject.FindProperty("_maxVelocityClampStrength");
        overrideFrictionType = serializedObject.FindProperty("_frictionType");
    }

    public override void OnInspectorGUI()
    {
        // Always update the serializedObject at the beginning of OnInspectorGUI
        serializedObject.Update();

        // --- Draw the fields that are always visible ---
        EditorGUILayout.PropertyField(overrideProperties);

        // --- Conditionally draw the override fields ---
        // Check the boolean value of the _overrideProperties field.
        if (!overrideProperties.boolValue)
        {
            EditorGUILayout.LabelField("Override Values", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(overrideUseGravity);
            EditorGUILayout.PropertyField(overrideGravityMultiplier);
            EditorGUILayout.PropertyField(overrideMass);
            EditorGUILayout.PropertyField(overrideMassScale);
            EditorGUILayout.PropertyField(overrideLinearDrag);
            EditorGUILayout.PropertyField(overrideAngularDrag);
            EditorGUILayout.PropertyField(overrideKineticFriction);
            EditorGUILayout.PropertyField(overrideStaticFriction);
            EditorGUILayout.PropertyField(overrideMaxVelocity);
            EditorGUILayout.PropertyField(overrideMaxVelocityClampStrength);
            EditorGUILayout.PropertyField(overrideFrictionType);

            EditorGUI.indentLevel--;
        }
        else
        {
            EditorGUILayout.PropertyField(scriptableObjectProperties);
        }

        // Always apply pending modifications at the end of OnInspectorGUI
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
