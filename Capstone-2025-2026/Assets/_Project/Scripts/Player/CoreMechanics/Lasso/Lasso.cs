using FMODUnity;
using NodeCanvas.Tasks.Actions;
using System;
using Unity.Cinemachine;
using UnityEngine;

public class Lasso : MonoBehaviour
{
    [Header("External Components")]
    [field: SerializeField] public Transform HoldPos { get; private set; }
    [field: SerializeField] public Transform PlayerCamLookPos { get; private set; }
    [field: SerializeField] public Transform CinemachineBrain { get; private set; }
    [field: SerializeField] public Camera PlayerCam { get; private set; }

    [SerializeField] private GameObject lassoGrabVisualIndicator;

    [Header("Lasso Properties")]
    [SerializeField] private float reelIncrement = 2f;
    [SerializeField] private float minLassoRange = 1f;
    [SerializeField] private float maxLassoRange = 25f;
    [SerializeField] private float centerStrength = 250f;
    [SerializeField, Range(0, 1)] private float lookAtStrength = 0.5f;
    [SerializeField, Range(0, 0.2f)] private float lookAtDamping = 0.05f;
    [SerializeField] private float rotationalDampingStrength = 0.5f;
    [SerializeField] private float maxLassoStrength;
    [SerializeField] private float maxLassoTorque;
    [SerializeField] private bool useSizeScale;
    [SerializeField, Range(0, 1)] private float angularVelMultiplier = 1f;

    [Header("Aim Assist Properties")]
    [SerializeField] private AimAssistType aimAssistType;
    [SerializeField] private float aimAssistBufferRadius = 1.5f;
    [SerializeField] private bool useAimOutline = true;
    [SerializeField] private bool usePickupOutline = true;
    [field: SerializeField] public bool usePhysicsLasso { get; set; } = true;
    [field: SerializeField] public bool usePhysicsTorque { get; private set; } = true;

    [Header("Swinging")]
    [SerializeField] private float swingJumpForce = 5f;
    [SerializeField] private Transform forwardRef;

    [Header("Rotation")]
    [field: SerializeField] public CinemachineInputAxisController camInputController { get; private set; }
    public enum RotationMode
    {
        ScreenSpace,      //use cam up and right axes
        SmartGimbal,      //world up, camera sideways axis
        ObjectRelative    //object's axes
    }


    [Header("Free Rotation")]
    [SerializeField] private RotationMode rotationMode = RotationMode.ScreenSpace;
    [SerializeField] private float degreesPerSecond = 180f;
    [SerializeField] private float mkSensMultiplier = 0.05f;
    private Quaternion rotationOffset;
    private bool rotating;

    [Header("Lifting")]
    [SerializeField] private float liftSpeed = 1.5f;
    [field: SerializeField] public float maxLiftHeight { get; private set; }
    [field: SerializeField] public float minHeightAboveGround { get; private set; }
    public float LiftSpeed => liftSpeed;
    [SerializeField] private float maxBelowGround = -3f;
    [SerializeField] private float groundCheckDistance = 100f;
    [SerializeField] private LayerMask groundLayers;
    private float _currentLiftOffset;


    [Header("Internal Variables")]
    private AimAssist _aimAssist;
    private PlayerSwing _swingController;
    private Transform _snaredObjTransform;
    private Transform _nearestGrabPoint;
    private Vector3 _attachPointLocal;
    private Vector3 _localFaceNormal;

    //for object manipulation mode
    private Vector3 _cachedAttachLocal;
    private Vector3 _cachedLocalFaceNormal;
    private float _cachedAnchorDist;
    private bool _hasCachedAttach;

    [Header("Getters")]
    public Prop SnaredObject { get; private set; }
    public PlayerMovement PlayerController { get; private set; }
    public Vector3 HitPos
    {
        get => _snaredObjTransform.TransformPoint(_attachPointLocal);
        set => _attachPointLocal = _snaredObjTransform.InverseTransformPoint(value);
    }
    public float AnchorDist { get; private set; }
    public float MaxLassoRange => maxLassoRange;

    public event Action OnObjectHit;
    public event Action OnNPCHit;
    public event Action OnLassoReleased;

    #region Unity Functions

    private void Awake()
    {
        PlayerController = GetComponent<PlayerMovement>();
        _swingController = GetComponent<PlayerSwing>();
        _aimAssist = new AimAssist();
    }

    private void Update()
    {
        if (CheckIfStandingOn() || Vector3.Distance(_snaredObjTransform.position, transform.position) > maxLassoRange * 1.5f)
        {
            HandleObjectReleased();
        }

        //CheckNearbyTargets(true);
        if (SnaredObject != null) SetVerticalAnchor(false);
    }

    #endregion

    #region Helper Functions

    public bool CheckIfStandingOn()
    {
        Transform standingOn = PlayerController.IsGrounded();
        bool nulled = _snaredObjTransform == null || SnaredObject == null;

        if (standingOn != null)
        {
            bool sameAsHeld = standingOn == _snaredObjTransform;
            bool allowedToHoldWhileStanding = standingOn.TryGetComponent(out Prop prop) && !prop.CanHoldWhileStandingOn;

            if (sameAsHeld && allowedToHoldWhileStanding) return true;
        }

        if (nulled) return true;
        return false;
    }

    public void SetLayer(Prop obj, bool original)
    {
        if (original) obj.gameObject.tag = obj.OriginalTag;
        else obj.gameObject.tag = gameObject.tag;
    }

    private Vector3 GetCameraWorldOffset()
    {
        CinemachineCameraOffset cameraOffset = CinemachineBrain.GetComponent<CinemachineCameraOffset>();
        if (cameraOffset != null)
        {
            Vector3 localOffset = cameraOffset.Offset;
            return PlayerCam.transform.TransformDirection(localOffset);
        }
        return Vector3.zero;
    }

    private Vector3 GetBaseTargetPos()
    {
        Ray ray = PlayerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        Vector3 camOffset = GetCameraWorldOffset();
        return PlayerCamLookPos.position + camOffset + (ray.direction * AnchorDist);
    }

    public Vector3 GetAnchoredCenterOfScreen()
    {
        Vector3 maxDistancePos = GetBaseTargetPos();
        maxDistancePos += Vector3.up * _currentLiftOffset;

        return maxDistancePos;
    }


    public void CheckNearbyTargets(bool showGrabPoints, float range)
    {
        Prop targetProp = null;

        if (useAimOutline)
        {
            RaycastHit? hit = _aimAssist.GetAssistHitPoint(PlayerCam, PlayerCamLookPos.position, range, aimAssistType, aimAssistBufferRadius);
            if (hit.HasValue)
            {
                targetProp = hit.Value.transform.GetComponentInParent<Prop>();
                targetProp.SetOutlineColour(Color.green);
                targetProp.SetOutlineWidth(2f);

                if (showGrabPoints || targetProp.IsTetherPulled)
                {
                    Transform closestPointTransform = targetProp.CheckNearestGrabPoint(hit.Value.point);
                    lassoGrabVisualIndicator.transform.position = closestPointTransform != null ? closestPointTransform.position : hit.Value.point;
                }
            }
            else
            {
                lassoGrabVisualIndicator.transform.position = transform.position;
            }
        }

        if (SnaredObject != null)
        {
            targetProp = SnaredObject;
            SnaredObject.SetOutlineColour(Color.cyan);
            SnaredObject.SetOutlineWidth(4f);
        }

        bool targetPropExists = targetProp != null;
        bool showIndicator = showGrabPoints || (targetPropExists ? targetProp.IsTetherPulled : false);
        lassoGrabVisualIndicator.SetActive(targetPropExists && SnaredObject == null && showIndicator);
        _aimAssist.HighlightSelectedProp(targetProp, false);

        //MVG BRAEDEN INPUT STUFF
        ContextPrompts.Instance.LookingAtObject(targetPropExists);
    }

    public void BeginCenterPivot()
    {
        if (SnaredObject == null || _snaredObjTransform == null) return;

        _cachedAttachLocal = _attachPointLocal;
        _cachedLocalFaceNormal = _localFaceNormal;
        _cachedAnchorDist = AnchorDist;
        _hasCachedAttach = true;

        _attachPointLocal = Vector3.zero;
        _localFaceNormal = Vector3.zero;

        AnchorDist = Mathf.Clamp(Vector3.Distance(_snaredObjTransform.position, PlayerCamLookPos.position), minLassoRange, maxLassoRange);
    }

    public void RestorePivot()
    {
        if (!_hasCachedAttach || SnaredObject == null || _snaredObjTransform == null) return;

        Transform nearest = SnaredObject.CheckNearestGrabPoint(PlayerCamLookPos.position);

        if (nearest != null)
        {
            AnchorDist = Mathf.Clamp(Vector3.Distance(nearest.position, PlayerCamLookPos.position), minLassoRange, maxLassoRange);
            _attachPointLocal = SnaredObject.transform.InverseTransformPoint(nearest.position);
            _localFaceNormal = SnaredObject.transform.InverseTransformDirection(nearest.forward);
        }
        else
        {
            _attachPointLocal = _cachedAttachLocal;
            _localFaceNormal = _cachedLocalFaceNormal;
            AnchorDist = _cachedAnchorDist;
            _hasCachedAttach = false;
        }
    }

    #endregion

    #region Start Lasso

    public void SetupHeldProp(Prop newProp, RaycastHit? hit)
    {
        //clear old reference if it exists
        if (SnaredObject != null)
        {
            SnaredObject.OnPropDestroyed -= HandleObjectReleased;
            SnaredObject.ActivateOutline(false);
        }

        SnaredObject = newProp;
        _snaredObjTransform = newProp.transform;
        _snaredObjTransform.gameObject.tag = gameObject.tag;
        if (usePickupOutline) SnaredObject.ActivateOutline(true);

        GetHoldPoint(newProp, hit, true);
        GetStartGrabRotation();

        SnaredObject.OnPropDestroyed += HandleObjectReleased;
    }

    public void HandleLassoStart()
    {
        RaycastHit? hit = _aimAssist.GetAssistHitPoint(PlayerCam, PlayerCamLookPos.position, maxLassoRange, aimAssistType, aimAssistBufferRadius);
        if (hit.HasValue)
        {
            RaycastHit actualHit = hit.Value;
            Prop prop = actualHit.transform.GetComponentInParent<Prop>();

            if (prop.transform == PlayerController.IsGrounded()) return;

            SetupHeldProp(prop, hit.Value);

            prop.OnSnare(this);

            if (prop.TryGetComponent(out IActivatable activatable))
            {
                activatable.Activate();
            }

            if (prop.TryGetComponent(out PickupNPCProp npc)) //used to be out IPNC npc for mama
            {
                OnNPCHit?.Invoke();
            }

            OnObjectHit?.Invoke();
            //AudioManager.Instance.PlaySFX(AudioManager.Instance.Thrown, 5, 1);
            RuntimeManager.PlayOneShot("event:/LassoStart", transform.position);
        }
    }

    private void GetHoldPoint(Prop prop, RaycastHit? hit, bool useGrabPoint)
    {
        if (hit == null)
        {
            _attachPointLocal = prop.transform.InverseTransformPoint(prop.transform.position);
            AnchorDist = Mathf.Clamp(Vector3.Distance(prop.transform.position, PlayerCamLookPos.position), minLassoRange, maxLassoRange);
            return;
        }

        _nearestGrabPoint = prop.CheckNearestGrabPoint(hit.Value.point);

        if (useGrabPoint)
        {
            if (_nearestGrabPoint != null)
            {
                AnchorDist = Mathf.Clamp(Vector3.Distance(_nearestGrabPoint.position, PlayerCamLookPos.position), minLassoRange, maxLassoRange);
                _attachPointLocal = prop.transform.InverseTransformPoint(_nearestGrabPoint.position);
                _localFaceNormal = prop.transform.InverseTransformDirection(_nearestGrabPoint.forward);
            }

            else
            {
                AnchorDist = Mathf.Clamp(Vector3.Distance(prop.transform.position, PlayerCamLookPos.position), minLassoRange, maxLassoRange);
                _attachPointLocal = prop.transform.InverseTransformPoint(hit.Value.point);

                //AnchorDist = Mathf.Clamp(Vector3.Distance(hit.transform.position, PlayerCamLookPos.position), minLassoRange, maxLassoRange);
                //_attachPointLocal = prop.transform.InverseTransformPoint(hit.transform.position);
            }
        }
        else
        {
            AnchorDist = Mathf.Clamp(Vector3.Distance(hit.Value.transform.position, PlayerCamLookPos.position), minLassoRange, maxLassoRange);
            _attachPointLocal = prop.transform.InverseTransformPoint(hit.Value.transform.position);
        }
    }

    public void GetStartGrabRotation()
    {
        Vector3 targetDir = (PlayerCamLookPos.position - SnaredObject.transform.position).normalized;
        float radians = Mathf.Atan2(targetDir.x, targetDir.z);
        float degrees = radians * Mathf.Rad2Deg;
        Quaternion lookRotation = Quaternion.Euler(0f, degrees, 0f);

        rotationOffset = Quaternion.Inverse(lookRotation) * SnaredObject.transform.rotation;
        bufferedTargetRotation = SnaredObject.transform.rotation;
    }

    #endregion

    #region Hold Object At Center

    public void MoveObjectToPos(Vector3 desiredPos)
    {
        if (usePhysicsLasso)
        {
            Vector3 attachPointWorld = SnaredObject.transform.TransformPoint(_attachPointLocal);
            Vector3 pointVelocity = SnaredObject.Rb.GetPointVelocity(attachPointWorld);
            Vector3 displacement = desiredPos - attachPointWorld;

            Vector3 linearForce = CalculateLinearForce(displacement, pointVelocity);
            Vector3 torqueForce = CalculateTorqueForce(attachPointWorld, linearForce);
            Vector3 lookAtTorque = CalculateLookAtTorque();

            Vector3 totalTorque = torqueForce + lookAtTorque;

            if (useSizeScale)
            {
                float effectiveMassScale = CalculateScale();
                linearForce /= effectiveMassScale;
                totalTorque /= effectiveMassScale;
            }

            SnaredObject.ApplyForceInDirection(linearForce.normalized, linearForce.magnitude, ForceMode.Force, transform);

            if (usePhysicsTorque)
            {
                SnaredObject.Rb.AddTorque(totalTorque, ForceMode.Force);
            }

            SnaredObject.Rb.angularVelocity *= angularVelMultiplier; //stop excessive spin
        }
        else
        {
            if (SnaredObject == null) return;

            float currentDist = Vector3.Distance(desiredPos, HitPos);
            float currentSpeed = Mathf.SmoothStep(0f, centerStrength * 10f, currentDist / 5f) * Time.fixedDeltaTime;
            Vector3 direction = desiredPos - HitPos;
            SnaredObject.Rb.linearVelocity = direction.normalized * currentSpeed;
        }

        //if (!rotating && !SnaredObject.IsTetherPulled) LookAtPlayer(); //this causes issues
    }
    private Quaternion bufferedTargetRotation;
    public void LookAtPlayer()
    {
        Vector3 targetDir = (PlayerCamLookPos.position - SnaredObject.transform.position).normalized;
        Vector3 currentDir = SnaredObject.transform.forward;

        float radians = Mathf.Atan2(targetDir.x, targetDir.z);
        float degrees = radians * Mathf.Rad2Deg;

        //get direction to rotate (toAngleAxis), get amount to rotate (angleDegrees)
        Quaternion current = SnaredObject.transform.rotation;
        Quaternion target = Quaternion.Euler(0f, degrees, 0f) * rotationOffset;

        bufferedTargetRotation = Quaternion.Slerp(
            bufferedTargetRotation,
            target,
            Time.fixedDeltaTime * 10f
        );

        Quaternion delta = bufferedTargetRotation * Quaternion.Inverse(current);
        delta.ToAngleAxis(out float angleDeg, out Vector3 axis);
        if (angleDeg > 180f) angleDeg -= 360f;

        float angleRad = angleDeg * Mathf.Deg2Rad;
        float rotationAccel = angleRad * centerStrength * 5f * Time.fixedDeltaTime;

        SnaredObject.Rb.angularVelocity = axis.normalized * rotationAccel;
    }


    private Vector3 CalculateLinearForce(Vector3 displacement, Vector3 pointVelocity)
    {
        //linear force
        float springStrength = centerStrength;
        Vector3 springForce = springStrength * displacement; //F = -springRate * displacement
        float damping = 2f * Mathf.Sqrt(springStrength * SnaredObject.Rb.mass); //critical damping = 2 * sqrt(springRate * mass)
        Vector3 dampingForce = -pointVelocity * damping;
        Vector3 totalForce = springForce + dampingForce;

        totalForce = totalForce.normalized * Mathf.Clamp(totalForce.magnitude, 0f, maxLassoStrength);
        return totalForce;
    }

    private Vector3 CalculateTorqueForce(Vector3 attachPointWorld, Vector3 linearForce)
    {
        Vector3 r = attachPointWorld - SnaredObject.Rb.worldCenterOfMass;
        float leverArmLength = (attachPointWorld - SnaredObject.Rb.worldCenterOfMass).magnitude;
        float scale = 1f / (1f + leverArmLength);

        Vector3 dampingTorque = -SnaredObject.Rb.angularVelocity * rotationalDampingStrength;
        Vector3 correctiveTorque = Vector3.Cross(r, linearForce);
        Vector3 finalTorque = correctiveTorque * scale;

        finalTorque = finalTorque.normalized * Mathf.Clamp(finalTorque.magnitude, 0f, maxLassoTorque);

        if (SnaredObject.IsTouchingSurface) return finalTorque + dampingTorque;
        else return finalTorque;
    }

    private Vector3 CalculateLookAtTorque()
    {
        Vector3 lookAtSpringTorque = Vector3.zero;
        Vector3 lookAtDampingTorque = Vector3.zero;

        if (_localFaceNormal != Vector3.zero)
        {
            Vector3 worldFaceNormal = SnaredObject.transform.TransformDirection(_localFaceNormal);
            Vector3 toPlayer = (PlayerCamLookPos.position - worldFaceNormal).normalized;
            lookAtSpringTorque = Vector3.Cross(worldFaceNormal, toPlayer) * lookAtStrength;
            lookAtDampingTorque = -SnaredObject.Rb.angularVelocity * lookAtDamping;
        }

        return lookAtSpringTorque + lookAtDampingTorque;
    }

    private float CalculateScale()
    {
        Vector3 scale = SnaredObject.transform.localScale;
        float effectiveMassScale = scale.x * scale.y * scale.z;

        effectiveMassScale = Mathf.Max(1.0f, effectiveMassScale);

        return effectiveMassScale;
    }

    #endregion

    #region Rotation

    public void RotateWithInput(Vector2 input, bool centerPivot)
    {
        if (SnaredObject == null || SnaredObject.Rb == null) return;

        float stepX = input.x * degreesPerSecond * Time.fixedDeltaTime;
        float stepY = input.y * degreesPerSecond * Time.fixedDeltaTime;

        Quaternion rotationStep = CalculateRotationStep(stepX, stepY);
        rotationStep = GetConstrainedRotation(rotationStep, SnaredObject.Rb.constraints);

        rotationStep.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f) angle -= 360f; // normalize

        Vector3 targetAngularVelocity = axis * Mathf.Deg2Rad * angle / Time.fixedDeltaTime;
        Vector3 angularVelocityError = targetAngularVelocity - SnaredObject.Rb.angularVelocity;
        Vector3 correctiveTorque = angularVelocityError / Time.fixedDeltaTime;

        SnaredObject.Rb.AddTorque(correctiveTorque, ForceMode.Acceleration);
    }

    private Quaternion CalculateRotationStep(float stepX, float stepY)
    {
        Quaternion yawRot = Quaternion.identity;
        Quaternion pitchRot = Quaternion.identity;

        switch (rotationMode)
        {
            case RotationMode.ScreenSpace:
                yawRot = Quaternion.AngleAxis(stepX, PlayerCam.transform.up);
                pitchRot = Quaternion.AngleAxis(-stepY, PlayerCam.transform.right);
                break;

            case RotationMode.SmartGimbal:
                Vector3 camForwardFlat = Vector3.ProjectOnPlane(PlayerCam.transform.forward, Vector3.up).normalized;
                if (camForwardFlat.sqrMagnitude < 0.01f)
                    camForwardFlat = PlayerCam.transform.up.y > 0 ? Vector3.forward : Vector3.back;
                Vector3 camRightFlat = Vector3.Cross(Vector3.up, camForwardFlat).normalized;

                yawRot = Quaternion.AngleAxis(stepX, Vector3.up);
                pitchRot = Quaternion.AngleAxis(-stepY, camRightFlat);
                break;

            case RotationMode.ObjectRelative:
                yawRot = Quaternion.AngleAxis(stepX, SnaredObject.transform.up);
                pitchRot = Quaternion.AngleAxis(-stepY, SnaredObject.transform.right);
                break;
        }

        return pitchRot * yawRot;
    }

    private Quaternion GetConstrainedRotation(Quaternion rot, RigidbodyConstraints constraints)
    {
        rot.ToAngleAxis(out float angle, out Vector3 axis);

        if (Mathf.Abs(angle) < Mathf.Epsilon || axis == Vector3.zero)
            return Quaternion.identity;

        //use constraints to remove axis rotation components
        if ((constraints & RigidbodyConstraints.FreezeRotationX) != 0) axis.x = 0;
        if ((constraints & RigidbodyConstraints.FreezeRotationY) != 0) axis.y = 0;
        if ((constraints & RigidbodyConstraints.FreezeRotationZ) != 0) axis.z = 0;

        //if everything is zero'd out, no rotation
        if (axis == Vector3.zero) return Quaternion.identity;

        //otherwise, recreate the quaternion
        return Quaternion.AngleAxis(angle, axis.normalized);
    }

    public void SetRotating(bool rotate)
    {
        rotating = rotate;
        GetStartGrabRotation();
    }

    #endregion

    #region Anchor Adjustment

    public void MoveAnchorPointZ(float scrollInput)
    {
        if (Mathf.Approximately(scrollInput, 0f)) return;

        AnchorDist += Mathf.Sign(scrollInput) * reelIncrement;
        AnchorDist = Mathf.Round(AnchorDist / reelIncrement) * reelIncrement;
        AnchorDist = Mathf.Clamp(AnchorDist, minLassoRange, maxLassoRange);
    }

    public void MoveAnchorPointY(float lookInputY)
    {
        if (SuppressLiftInput) return;
        if (Mathf.Approximately(lookInputY, 0f)) return;

        _currentLiftOffset += lookInputY * liftSpeed * Time.deltaTime;
    }

    /// <summary>Current lift offset of the held object above the camera crosshair target.</summary>
    public float CurrentLiftOffset => _currentLiftOffset;

    public float GetMinLiftOffset()
    {
        if (SnaredObject == null || _snaredObjTransform == null) return 0f;
        Vector3 baseTargetPos = GetBaseTargetPos();
        float absoluteMinY = GetMinimumLiftHeight();
        return absoluteMinY - baseTargetPos.y;
    }

    /// <summary>
    /// When true, MoveAnchorPointY is a no-op. Set by the camera system when it is
    /// managing vertical object positioning via pitch + lift offset directly.
    /// </summary>
    public bool SuppressLiftInput { get; set; } = false;

    /// <summary>Directly adds to the lift offset (used by camera system to lower object while camera stays clamped).</summary>
    public void AddLiftOffset(float delta)
    {
        _currentLiftOffset += delta;
    }

    public void SetVerticalAnchor(bool setMinimum)
    {
        //get base position
        Vector3 baseTargetPos = GetBaseTargetPos();

        //get world space limits
        float characterFeetY = transform.position.y - 1f;
        float absoluteMaxY = characterFeetY + maxLiftHeight;
        float absoluteMinY = GetMinimumLiftHeight();

        //convert those limits relative to the base position
        float maxAllowedOffset = absoluteMaxY - baseTargetPos.y;
        float minAllowedOffset = absoluteMinY - baseTargetPos.y;

        if (setMinimum) _currentLiftOffset = minAllowedOffset;

        _currentLiftOffset = Mathf.Clamp(_currentLiftOffset, minAllowedOffset, maxAllowedOffset);
    }

    public float GetMinimumLiftHeight()
    {
        if (SnaredObject == null || _snaredObjTransform == null)
            return 0f;

        if (!TryGetObjectBounds(SnaredObject.gameObject, out Bounds bounds))
            return 0f;

        Vector3 boxOrigin = new Vector3(bounds.center.x, bounds.min.y + 0.01f, bounds.center.z);
        Vector3 halfExtents = new Vector3(bounds.extents.x * 0.9f, 0.01f, bounds.extents.z * 0.9f);

        float groundYBelowObject = float.NegativeInfinity;

        if (Physics.BoxCast(boxOrigin, halfExtents, Vector3.down, out RaycastHit hit, Quaternion.identity, groundCheckDistance, groundLayers))
        {
            groundYBelowObject = hit.point.y;
        }

        float objectHalfHeight = bounds.extents.y;

        //ground found under object -> minHeightAboveGround + hit
        if (!float.IsNegativeInfinity(groundYBelowObject))
        {
            return groundYBelowObject + objectHalfHeight + minHeightAboveGround;
        }

        //fallback -> object is floating above the void
        float playerFeetY = transform.position.y - 1f;
        return playerFeetY - maxBelowGround;
    }

    bool TryGetObjectBounds(GameObject obj, out Bounds bounds)
    {
        bounds = new Bounds();

        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            bounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
                bounds.Encapsulate(colliders[i].bounds);

            return true;
        }

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return true;
        }

        return false;
    }



    #endregion

    #region Swinging

    public void HandleSwingSetup()
    {
        _attachPointLocal = _snaredObjTransform.InverseTransformPoint(GetNearestSwingPoint().position);
        _swingController.StartSwing(HitPos, PlayerController.Rb.linearVelocity, AnchorDist);
    }

    public void SwingJumpBoost()
    {
        HandleObjectReleased();
        PlayerController.Rb.AddForce((Vector3.up + PlayerCam.transform.forward).normalized * swingJumpForce, ForceMode.Impulse);
    }

    private Transform GetNearestSwingPoint()
    {
        Transform nearestSwingPoint = null;
        float currentNearestDist = float.MaxValue;

        foreach (Transform child in _snaredObjTransform)
        {
            if (child.TryGetComponent(out SwingPoint swingPoint))
            {
                float dist = Vector3.Distance(HitPos, swingPoint.transform.position);

                if (dist < currentNearestDist)
                {
                    currentNearestDist = dist;
                    nearestSwingPoint = child;
                }
            }
        }

        if (nearestSwingPoint == null && _snaredObjTransform.TryGetComponent(out SwingPoint parentSwingPoint))
        {
            return parentSwingPoint.transform;
        }

        return nearestSwingPoint;
    }

    #endregion

    #region Release
    public void HandleObjectReleased()
    {
        if (SnaredObject == null) return;

        _currentLiftOffset = 0f;
        SnaredObject.OnPropDestroyed -= HandleObjectReleased;
        SnaredObject.ActivateOutline(false);
        SnaredObject.OnRelease();
        _snaredObjTransform.gameObject.tag = SnaredObject.OriginalTag;
        _snaredObjTransform = null;
        SnaredObject = null;
        _localFaceNormal = Vector3.zero;

        lassoGrabVisualIndicator.SetActive(false);
        OnLassoReleased?.Invoke();
    }

    public void HandleHold()
    {
        if (SnaredObject == null) return;

        SnaredObject.ActivateOutline(false);
        SnaredObject = null;
        _snaredObjTransform = null;
        _localFaceNormal = Vector3.zero;
        lassoGrabVisualIndicator.SetActive(false);
    }

    #endregion

}