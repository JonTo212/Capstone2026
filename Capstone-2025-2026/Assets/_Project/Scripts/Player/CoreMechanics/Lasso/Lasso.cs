using NodeCanvas.Tasks.Actions;
using System;
using System.Collections;
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
    [SerializeField] private float throwStrength = 25f;
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
    [SerializeField] private bool useGrabPointsForHold = false;
    [field: SerializeField] public bool usePhysicsLasso { get; set; } = true;
    [field: SerializeField] public bool usePhysicsTorque { get; private set; } = true;

    [Header("Swinging")]
    [SerializeField] private float swingJumpForce = 5f;
    [SerializeField] private Transform forwardRef;

    [Header("Rotation")]
    [field: SerializeField] public bool useSnapRotation { get; private set; } = false;
    [field: SerializeField] public CinemachineInputAxisController camInputController { get; private set; }
    public bool Rotated { get; private set; }

    [Header("Internal Variables")]
    private AimAssist _aimAssist;
    private PlayerSwing _swingController;
    private Transform _snaredObjTransform;
    private Transform _nearestGrabPoint;
    private Vector3 _attachPointLocal;
    private Vector3 _localFaceNormal;
    private Joint swingJoint;

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

    public event Action OnObjectHit;
    public event Action OnNPCHit;
    public event Action OnLassoReleased;


    [Header("Object Manipulation")]
    [SerializeField] private float liftSpeed = 5f; // How fast it moves up/down
    [SerializeField] private float maxLiftHeight = 3f; // Max distance up/down from center

    private float _currentLiftOffset; // The actual offset value
    private IInputAxisReader _yAxisReader; // To read input

    #region Unity Functions
    private void Awake()
    {
        PlayerController = GetComponent<PlayerMovement>();
        _swingController = GetComponent<PlayerSwing>();
        _aimAssist = new AimAssist();
    }

    private void Start()
    {
        if (camInputController != null)
        {
            foreach (var c in camInputController.Controllers)
            {
                if (c.Name == "Look Orbit Y") _yAxisReader = c.Input;
            }
        }
    }
    private void Update()
    {
        if (CheckIfBreak())
        {
            HandleObjectReleased();
        }

        CheckNearbyTargets();

        // NEW LOGIC: Adjust Lift Offset based on Input
        if (SnaredObject != null && _yAxisReader != null)
        {
            float yInput = PlayerController.PlayerInput.LookInput.y;

            if (Mathf.Abs(yInput) > 0.001f)
            {
                // Add to offset
                _currentLiftOffset += yInput * liftSpeed * Time.deltaTime;

                // Clamp it so you can't lift it infinitely high
                _currentLiftOffset = Mathf.Clamp(_currentLiftOffset, -maxLiftHeight, maxLiftHeight);
            }
        }
    }

    #endregion

    #region Helper Functions

    public bool CheckIfBreak()
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

    public void ResetLayer(Prop obj)
    {
        obj.gameObject.tag = obj.OriginalTag;
    }

    public void SetToNoCollisionLayer(Prop obj)
    {
        obj.gameObject.tag = gameObject.tag;
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

    public Vector3 GetAnchoredCenterOfScreen()
    {
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        Ray screenRay = PlayerCam.ScreenPointToRay(screenCenter);

        Vector3 camOffset = GetCameraWorldOffset();
        Ray ray = new Ray(PlayerCamLookPos.position + camOffset, screenRay.direction);

        // Base position (Center of screen at distance)
        Vector3 maxDistancePos = PlayerCamLookPos.position + camOffset + ray.direction * AnchorDist;

        // NEW: Apply the vertical offset relative to the Camera's Up direction
        // This ensures "Up" on the stick means "Up" on the screen.
        maxDistancePos += PlayerCam.transform.up * _currentLiftOffset;

        return maxDistancePos;
    }

    private void CheckNearbyTargets()
    {
        Prop targetProp = null;

        if (useAimOutline)
        {
            RaycastHit? hit = _aimAssist.GetAssistHitPoint(PlayerCam, PlayerCamLookPos.position, maxLassoRange * 1.2f, aimAssistType, aimAssistBufferRadius);
            if (hit.HasValue)
            {
                targetProp = hit.Value.transform.GetComponentInParent<Prop>();
                targetProp.SetOutlineColour(Color.green);
                targetProp.SetOutlineWidth(2f);

                Transform closestPointTransform = targetProp.CheckNearestGrabPoint(hit.Value.point);

                if (closestPointTransform != null)
                {
                    lassoGrabVisualIndicator.transform.position = closestPointTransform.position;
                }
                else
                {
                    lassoGrabVisualIndicator.transform.position = hit.Value.point;
                }
            }

        }


        if (SnaredObject != null)
        {
            targetProp = SnaredObject;
            SnaredObject.SetOutlineColour(Color.cyan);
            SnaredObject.SetOutlineWidth(4f);
        }

        lassoGrabVisualIndicator.SetActive(targetProp != null && SnaredObject == null);
        _aimAssist.HighlightSelectedProp(targetProp, false);

        bool targetPropExists = targetProp != null;

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
    public void HandleLassoStart()
    {
        RaycastHit? hit = _aimAssist.GetAssistHitPoint(PlayerCam, PlayerCamLookPos.position, maxLassoRange * 1.2f, aimAssistType, aimAssistBufferRadius);
        if (hit.HasValue)
        {
            RaycastHit actualHit = hit.Value;
            Prop prop = actualHit.transform.GetComponentInParent<Prop>();
            GetHoldPoint(prop, actualHit, useGrabPointsForHold);

            if (prop.transform == PlayerController.IsGrounded()) return;

            _snaredObjTransform = prop.transform;
            //SetUpConfigurableJoint(prop.Rb, HitPos, transform);
            SnaredObject = prop;
            _snaredObjTransform.gameObject.tag = gameObject.tag;
            prop.OnSnare();
            prop.OnPropDestroyed += HandleObjectReleased;
            if (usePickupOutline) SnaredObject.ActivateOutline(true);

            if (prop.TryGetComponent(out IActivatable activatable))
            {
                activatable.Activate();
            }

            if(prop.TryGetComponent(out INPC npc))
            {
                OnNPCHit?.Invoke();
            }

            OnObjectHit?.Invoke();
            AudioManager.Instance.PlaySFX(AudioManager.Instance.Thrown, 5, 1);
        }
    }

    private Transform jointAnchor;
    private ConfigurableJoint holdJoint;

    private void SetUpConfigurableJoint(Rigidbody rb, Vector3 attachmentPosition, Transform parent)
    {
        GameObject go = new GameObject("JointAnchor");
        go.transform.position = attachmentPosition;

        //this rb acts as anchor
        Rigidbody newRb = go.AddComponent<Rigidbody>();
        newRb.isKinematic = true;

        //make a new joint that uses jointdrive (like addForce in a direction, with damping)
        float damping = 2f * Mathf.Sqrt(centerStrength * PlayerController.Rb.mass);
        ConfigurableJoint joint = go.AddComponent<ConfigurableJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedBody = rb;
        joint.xDrive = NewJointDrive(centerStrength, damping);
        joint.yDrive = NewJointDrive(centerStrength, damping);
        joint.zDrive = NewJointDrive(centerStrength, damping);
        joint.slerpDrive = NewJointDrive(centerStrength, damping);
        joint.rotationDriveMode = RotationDriveMode.Slerp;
        joint.connectedAnchor = rb.transform.InverseTransformPoint(attachmentPosition);

        jointAnchor = go.transform;
        holdJoint = joint;
    }

    private JointDrive NewJointDrive(float force, float damping)
    {
        JointDrive drive = new JointDrive();
        drive.positionSpring = force;
        drive.positionDamper = damping;
        drive.maximumForce = Mathf.Infinity;
        return drive;
    }


    private void GetHoldPoint(Prop prop, RaycastHit hit, bool useGrabPoint)
    {
        _nearestGrabPoint = prop.CheckNearestGrabPoint(hit.point);

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
                AnchorDist = Mathf.Clamp(Vector3.Distance(hit.point, PlayerCamLookPos.position), minLassoRange, maxLassoRange);
                _attachPointLocal = prop.transform.InverseTransformPoint(hit.point);

                //AnchorDist = Mathf.Clamp(Vector3.Distance(hit.transform.position, PlayerCamLookPos.position), minLassoRange, maxLassoRange);
                //_attachPointLocal = prop.transform.InverseTransformPoint(hit.transform.position);
            }
        }
        else
        {
            AnchorDist = Mathf.Clamp(Vector3.Distance(hit.transform.position, PlayerCamLookPos.position), minLassoRange, maxLassoRange);
            _attachPointLocal = prop.transform.InverseTransformPoint(hit.transform.position);
        }
    }

    #endregion

    #region Hold Object At Center
    public void RotateHeldObject()
    {
        Quaternion targetRotation = Quaternion.LookRotation(HoldPos.forward, Vector3.up);
        SnaredObject.Rb.MoveRotation(targetRotation);
    }

    public void MoveObjectToPos(Vector3 desiredPos)
    {
        /*if (SnaredObject == null) return;
        if (jointAnchor != null)
        {
            jointAnchor.position = desiredPos;
        }

        float currentDist = Vector3.Distance(desiredPos, SnaredObject.transform.position);
        float speedFactor = Mathf.Clamp01(currentDist / maxLassoRange);
        float currentSpeed = Mathf.SmoothStep(0f, centerStrength, speedFactor);

        currentSpeed *= Time.fixedDeltaTime;
        Vector3 direction = desiredPos - SnaredObject.transform.position;

        SnaredObject.Rb.linearVelocity = direction.normalized * currentSpeed;*/

        
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

            //SnaredObject.Rb.AddForceAtPosition(linearForce, attachPointWorld, ForceMode.Force); //accel works because the damping already takes into account mass
            //if (SnaredObject.IsTouchingSurface) linearAcceleration = Vector3.ClampMagnitude(linearForce / effectiveMassScale, centerStrength);
            //if (!SnaredObject.IsTouchingSurface) SnaredObject.Rb.AddTorque(angularAcceleration, ForceMode.Acceleration);

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
            float currentSpeed = Mathf.SmoothStep(0f, centerStrength, currentDist / 5f) * Time.fixedDeltaTime;
            Vector3 direction = desiredPos - HitPos;
            SnaredObject.Rb.linearVelocity = direction.normalized * currentSpeed;
        }
    }

    public void LookAtPlayer()
    {
        Vector3 targetDir = (PlayerCamLookPos.position - SnaredObject.transform.position).normalized;
        Vector3 currentFaceDir = _localFaceNormal != Vector3.zero ? SnaredObject.transform.TransformDirection(_localFaceNormal) : SnaredObject.transform.forward;

        Quaternion deltaRot = Quaternion.FromToRotation(currentFaceDir, targetDir);
        deltaRot.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f) angle -= 360f;

        float currentAngularSpeed = Mathf.SmoothStep(0f, centerStrength * 5f, Mathf.Abs(angle) / 45f) * Time.fixedDeltaTime;
        SnaredObject.Rb.angularVelocity = axis.normalized * (Mathf.Sign(angle) * currentAngularSpeed * Mathf.Deg2Rad);
    }

    private Vector3 CalculateLinearForce(Vector3 displacement, Vector3 pointVelocity)
    {
        //linear force
        float springStrength = centerStrength / 12.5f;
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

    [Header("Free Rotation")]
    [SerializeField] private float degreesPerSecond = 180f;
    [SerializeField] private float mkSensMultiplier = 0.05f; //multiply this in if using m/k in the future

    public void RotateWithInput(Vector2 input, bool centerPivot)
    {
        if (SnaredObject == null || SnaredObject.Rb == null) return;

        float stepX = input.x * degreesPerSecond * Time.fixedDeltaTime;
        float stepY = input.y * degreesPerSecond * Time.fixedDeltaTime;

        Quaternion yawRot = Quaternion.AngleAxis(stepX, Vector3.up);
        Quaternion pitchRot = Quaternion.AngleAxis(-stepY, PlayerCam.transform.right);

        Quaternion rotationStep = pitchRot * yawRot;
        Quaternion nextRotation = rotationStep * SnaredObject.Rb.rotation;

        Vector3 pivotPoint = HitPos;
        Vector3 currentPos = SnaredObject.transform.position;
        Vector3 offsetFromPivot = currentPos - pivotPoint;
        Vector3 rotatedOffset = rotationStep * offsetFromPivot;
        Vector3 nextPosition = pivotPoint + rotatedOffset;


        /*if (centerPivot)
        {
            rotationStep.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f; // normalize

            Vector3 targetAngularVelocity = axis * Mathf.Deg2Rad * angle / Time.fixedDeltaTime;
            Vector3 angularVelocityError = targetAngularVelocity - SnaredObject.Rb.angularVelocity;
            Vector3 correctiveTorque = angularVelocityError / Time.fixedDeltaTime;

            SnaredObject.Rb.AddTorque(correctiveTorque, ForceMode.Acceleration);
        }
        else
        {*/
            SnaredObject.Rb.MoveRotation(nextRotation);
            SnaredObject.Rb.MovePosition(nextPosition);
            SnaredObject.Rb.angularVelocity = Vector3.zero;
        //}

        Rotated = true;
    }

    #endregion

    #region Snap Rotation

    [Header("Snap Rotation")]
    [SerializeField] private float snapAngle = 45f;
    [SerializeField] private float rotationSpring = 50f;
    [SerializeField] private float snapRatePerSecond = 360f;
    private float snapAccum;
    private float yaw;
    private float pitch;
    private Coroutine finishSnapRoutine;
    public Action OnSnapFinished;

    private Quaternion GetBaseLookRotation()
    {
        if (PlayerCam == null) return Quaternion.identity;

        //flatten the Y of camera's forward so it's only on XZ plane
        Vector3 lookDirection = PlayerCam.transform.forward;
        lookDirection.y = 0;

        //this just makes a rotation with the Z axis aligned with player's look direction
        return Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
    }

    public void InitializeRotationToClosestSnap()
    {
        if (SnaredObject == null || SnaredObject.Rb == null) return;

        Quaternion baseLookRotation = GetBaseLookRotation();
        Quaternion currentObjectRotation = SnaredObject.Rb.rotation;

        //simulate every angle from -180 to 180 and choose the closest one
        //just deriving from eulerAngles makes the pitch flip, this was the only way that worked
        int steps = Mathf.RoundToInt(360f / snapAngle);
        float bestYaw = 0f;
        float bestPitch = 0f;
        float bestAngle = float.MaxValue;

        for (int p = 0; p < steps; p++)
        {
            float candidatePitch = -180f + p * snapAngle;
            Quaternion pitchRot = Quaternion.AngleAxis(candidatePitch, Vector3.right);

            for (int y = 0; y < steps; y++)
            {
                float candidateYaw = -180f + y * snapAngle;
                Quaternion yawRot = Quaternion.AngleAxis(candidateYaw, Vector3.up);
                Quaternion candidateRotation = baseLookRotation * pitchRot * yawRot;

                float angleBetween = Quaternion.Angle(candidateRotation, currentObjectRotation);
                if (angleBetween < bestAngle)
                {
                    bestAngle = angleBetween;
                    bestYaw = candidateYaw;
                    bestPitch = candidatePitch;
                }
            }
        }

        yaw = Mathf.Round(bestYaw / snapAngle) * snapAngle;
        pitch = Mathf.Round(bestPitch / snapAngle) * snapAngle;
    }


    public void MaintainObjectRotation()
    {
        if (SnaredObject == null || SnaredObject.Rb == null) return;

        //look at player + accumulated rotation
        Quaternion baseLookRotation = GetBaseLookRotation();

        Quaternion yawRotation = Quaternion.AngleAxis(yaw, Vector3.up);
        Quaternion pitchRotation = Quaternion.AngleAxis(pitch, Vector3.right);

        Quaternion combinedTargetRotation = baseLookRotation * pitchRotation * yawRotation; //accumulatedSnapRotation;

        //rotation required to get to the desired rotation
        Quaternion delta = combinedTargetRotation * Quaternion.Inverse(SnaredObject.Rb.rotation);
        delta.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f) angle -= 360f;

        Vector3 angularDisplacement = axis.normalized * Mathf.Deg2Rad * angle;

        //spring + critical damping
        Vector3 springTorque = rotationSpring * angularDisplacement;
        Vector3 dampingTorque = -2f * Mathf.Sqrt(rotationSpring) * SnaredObject.Rb.angularVelocity;

        SnaredObject.Rb.AddTorque(springTorque + dampingTorque, ForceMode.Acceleration);
    }

    public void ApplySnapRotation(Vector3 axis)
    {
        if (SnaredObject == null || axis == Vector3.zero) return;

        if (axis == Vector3.up)  yaw += snapAngle;
        else if (axis == Vector3.down) yaw -= snapAngle;
        else if (axis == Vector3.right) pitch += snapAngle;
        else if (axis == Vector3.left) pitch -= snapAngle;

        Rotated = true;
        RodAnimations.Instance.PlayRandomSound();
    }

    public void StartFinishSnap()
    {
        if (finishSnapRoutine != null)
            StopCoroutine(finishSnapRoutine);

        finishSnapRoutine = StartCoroutine(FinishSnapCoroutine());
    }

    public void StopFinishSnap()
    {
        if (finishSnapRoutine != null)
        {
            StopCoroutine(finishSnapRoutine);
            finishSnapRoutine = null;
        }
    }

    private IEnumerator FinishSnapCoroutine()
    {
        if (SnaredObject == null)
            yield break;

        //runs until the angle's close enough to completion or cancelled externally
        while (true)
        {
            MaintainObjectRotation();

            Quaternion baseLook = GetBaseLookRotation();
            Quaternion yawRot = Quaternion.AngleAxis(yaw, Vector3.up);
            Quaternion pitchRot = Quaternion.AngleAxis(pitch, Vector3.right);
            Quaternion targetRot = baseLook * pitchRot * yawRot;

            float angle = Quaternion.Angle(SnaredObject.Rb.rotation, targetRot);

            if (angle < 1f)
                break;

            yield return new WaitForFixedUpdate();
        }

        finishSnapRoutine = null;
        OnSnapFinished?.Invoke();
    }

    public void ContinuousSnapStep(Vector3 axis)
    {
        if (SnaredObject == null) return;

        snapAccum += snapRatePerSecond * Time.fixedDeltaTime;
        while (snapAccum >= snapAngle)
        {
            snapAccum -= snapAngle;

            if (axis == Vector3.up) yaw += snapAngle;
            else if (axis == Vector3.down) yaw -= snapAngle;
            else if (axis == Vector3.right) pitch += snapAngle;
            else if (axis == Vector3.left) pitch -= snapAngle;

            Rotated = true;
        }
    }

    #endregion

    #region Anchor Adjustment

    public void MoveAnchorPoint(float scrollInput)
    {
        if (Mathf.Approximately(scrollInput, 0f)) return;

        AnchorDist += Mathf.Sign(scrollInput) * reelIncrement;
        AnchorDist = Mathf.Round(AnchorDist / reelIncrement) * reelIncrement;
        AnchorDist = Mathf.Clamp(AnchorDist, minLassoRange, maxLassoRange);
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

    #region Throw / Release

    public void HandleObjectThrow()
    {
        if (SnaredObject == null) return;

        Ray ray = PlayerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        SnaredObject.OnThrow(ray.direction, throwStrength);
        AudioManager.Instance.PlaySFX(AudioManager.Instance.Thrown, 5, 1);
        HandleObjectReleased();
    }

    public void HandleObjectReleased()
    {
        if (SnaredObject == null) return;

        if (swingJoint != null) Destroy(swingJoint);

        if (holdJoint != null)
        {
            Destroy(holdJoint);
            Destroy(jointAnchor.gameObject);
        }
        _currentLiftOffset = 0f;

        SnaredObject.OnPropDestroyed -= HandleObjectReleased;
        SnaredObject.ActivateOutline(false);
        SnaredObject.OnRelease();
        _snaredObjTransform.gameObject.tag = SnaredObject.OriginalTag;
        _snaredObjTransform = null;
        SnaredObject = null;
        _localFaceNormal = Vector3.zero;

        Rotated = false;
        lassoGrabVisualIndicator.SetActive(false);
        StopFinishSnap();
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

    #region Anchoring

    public void AnchorToObject()
    {
        _swingController.UpdateAnchorPoint(HitPos);
        //_swingController.ConstrainToRope();
    }

    public void HandleAnchorStart()
    {
        _swingController.SetRopeLength(maxLassoRange * 1.1f);
    }

    #endregion
}
