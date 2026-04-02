using FMODUnity;
using System;
using UnityEngine;

public class Lasso : MonoBehaviour
{
    [Header("External Components")]
    [field: SerializeField] public Transform HoldPos { get; private set; } //for lasso visuals
    [SerializeField] private GameObject lassoGrabVisualIndicator; //grab point indicator

    [Header("Lasso Properties")]
    [SerializeField] private float minLassoRange = 3f;
    [field: SerializeField] public float MaxLassoRange { get; private set; } = 25f;
    [SerializeField] private float lassoHoldStrength = 250f;
    [SerializeField] private bool useSizeScale;
    [SerializeField, Range(0, 1)] private float lookAtStrength = 0.5f;
    [SerializeField, Range(0, 0.2f)] private float lookAtDamping = 0.05f;
    [SerializeField] private float rotationalDampingStrength = 0.5f;
    [SerializeField] private float maxLassoStrength;
    [SerializeField] private float maxLassoTorque;
    [SerializeField, Range(0, 1)] private float angularVelMultiplier = 1f;

    [Header("Aim Assist Properties")]
    [SerializeField] private AimAssistType aimAssistType;
    [SerializeField] private float aimAssistBufferRadius = 1.5f;

    [Header("Swinging")]
    [SerializeField] private float swingJumpForce = 5f;
    [SerializeField] private Transform forwardRef;

    [Header("Lifting")]
    [SerializeField] private float liftSpeed = 1.5f;
    [field: SerializeField] public float maxLiftHeight { get; private set; }
    [field: SerializeField] public float minHeightAboveGround { get; private set; }
    [SerializeField] private float maxBelowGround = 3f;
    [SerializeField] private float groundCheckDistance = 100f;
    [SerializeField] private LayerMask groundLayers;

    [Header("Internal Variables")]
    private PlayerRefData _playerRefData;
    private AimAssist _aimAssist;
    private Camera _playerCam;
    private Transform _nearestGrabPoint;
    private Vector3 _attachPointLocal;
    private Vector3 _localFaceNormal;

    [Header("Getters")]
    public Prop SnaredObject { get; private set; }
    public Vector3 HitPos
    {
        get => SnaredObject.transform.TransformPoint(_attachPointLocal);
        set => _attachPointLocal = SnaredObject.transform.InverseTransformPoint(value);
    }
    public float AnchorDist { get; private set; }
    public float CurrentLiftOffset { get; private set; }
    public bool LookingAtAutoEquipTarget { get; private set; }
    public bool SuppressLiftInput { get; set; }

    public event Action OnObjectHit;
    public event Action OnNPCHit;
    public event Action OnLassoReleased;

    #region Unity Functions

    private void Awake()
    {
        _aimAssist = new AimAssist();
        _playerCam = Camera.main;
        _playerRefData = GetComponent<PlayerRefData>();
    }

    private void Update()
    {
        if (SnaredObject != null)
        {
            if (CheckIfStandingOn() || Vector3.Distance(SnaredObject.transform.position, transform.position) > MaxLassoRange * 1.5f)
            {
                _playerRefData.LassoTetherController.ClearHold();
                return;
            }

            SetVerticalAnchor(false);
        }
    }

    #endregion

    #region Helper Functions
    public bool CheckIfStandingOn()
    {
        Transform standingOn = _playerRefData.PlayerMovement.IsGrounded();

        if (standingOn != null)
        {
            bool sameAsHeld = standingOn == SnaredObject.transform;
            bool allowedToHoldWhileStanding = standingOn.TryGetComponent(out Prop prop) && !prop.CanHoldWhileStandingOn;

            if (sameAsHeld && allowedToHoldWhileStanding) return true;
        }

        return false;
    }

    private Vector3 GetBaseTargetPos()
    {
        Ray ray = _playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        float camToPlayerDist = Vector3.Distance(ray.origin, transform.position);

        return ray.origin + ray.direction * (AnchorDist + camToPlayerDist);
    }

    public Vector3 GetAnchoredCenterOfScreen()
    {
        Vector3 maxDistancePos = GetBaseTargetPos();
        maxDistancePos += Vector3.up * CurrentLiftOffset;

        return maxDistancePos;
    }

    public void HandleHighlight(bool showGrabPoints, float range)
    {
        Prop targetProp = null;

        RaycastHit? hit = _aimAssist.GetAssistHitPoint(_playerCam, transform.position, range, aimAssistType, aimAssistBufferRadius);
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
        LookingAtAutoEquipTarget = targetPropExists && (targetProp.GetComponentInChildren<SwingPoint>() != null || targetProp.TryGetComponent(out PickupNPCProp pu) || targetProp.TryGetComponent(out RopeCutsceneActivator rsc));

        //MVG BRAEDEN INPUT STUFF
        ContextPrompts.Instance.LookingAtObject(targetPropExists);
    }

    #endregion

    #region Start Lasso

    public void SetupHeldProp(Prop newProp, RaycastHit? hit)
    {
        if (SnaredObject != null)
        {
            SnaredObject.OnPropDestroyed -= HandleObjectReleased;
            SnaredObject.ActivateOutline(false);
        }

        SnaredObject = newProp;
        SnaredObject.ActivateOutline(true);

        GetHoldPoint(newProp, hit, true);
        SnaredObject.OnPropDestroyed += HandleObjectReleased;
    }

    public void HandleLassoStart()
    {
        RaycastHit? hit = _aimAssist.GetAssistHitPoint(_playerCam, transform.position, MaxLassoRange, aimAssistType, aimAssistBufferRadius);
        if (hit.HasValue)
        {
            RaycastHit actualHit = hit.Value;
            Prop prop = actualHit.transform.GetComponentInParent<Prop>();

            if (prop.transform == _playerRefData.PlayerMovement.IsGrounded()) return;

            SetupHeldProp(prop, hit.Value);

            prop.OnSnare(_playerRefData);

            if (prop.TryGetComponent(out PickupNPCProp npc))
            {
                OnNPCHit?.Invoke();
            }

            if (prop.TryGetComponent(out PluckOutProp po))
            {
                po.SaveDist(AnchorDist, Vector3.Distance(transform.position, po.transform.position));
            }

            OnObjectHit?.Invoke();
            RuntimeManager.PlayOneShot("event:/LassoStart", transform.position);
            PlayerActions.Instance.RumbleFor(0.2f, 0.4f, 0.1f);
        }
    }

    private void GetHoldPoint(Prop prop, RaycastHit? hit, bool useGrabPoint)
    {
        if (hit == null)
        {
            _attachPointLocal = prop.transform.InverseTransformPoint(prop.transform.position);
            AnchorDist = Mathf.Clamp(Vector3.Distance(prop.transform.position, transform.position), minLassoRange, MaxLassoRange);
            return;
        }

        _nearestGrabPoint = prop.CheckNearestGrabPoint(hit.Value.point);

        if (useGrabPoint)
        {
            if (_nearestGrabPoint != null)
            {
                AnchorDist = Mathf.Clamp(Vector3.Distance(_nearestGrabPoint.position, transform.position), minLassoRange, MaxLassoRange);
                _attachPointLocal = prop.transform.InverseTransformPoint(_nearestGrabPoint.position);
                _localFaceNormal = prop.transform.InverseTransformDirection(_nearestGrabPoint.forward);
            }

            else
            {
                AnchorDist = Mathf.Clamp(Vector3.Distance(hit.Value.point, transform.position), minLassoRange, MaxLassoRange);
                //AnchorDist = Mathf.Clamp(Vector3.Distance(prop.transform.position, transform.position), minLassoRange, MaxLassoRange); //midpoint hold
                _attachPointLocal = prop.transform.InverseTransformPoint(hit.Value.point);
            }
        }
        else
        {
            AnchorDist = Mathf.Clamp(Vector3.Distance(hit.Value.transform.position, transform.position), minLassoRange, MaxLassoRange);
            _attachPointLocal = prop.transform.InverseTransformPoint(hit.Value.transform.position);
        }
    }

    #endregion

    #region Hold Object At Center

    public void MoveObjectToPos(Vector3 desiredPos)
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
        SnaredObject.Rb.AddTorque(totalTorque, ForceMode.Force);

        SnaredObject.Rb.angularVelocity *= angularVelMultiplier; //stop excessive spin
    }

    #endregion

    #region Lasso Math
    private Vector3 CalculateLinearForce(Vector3 displacement, Vector3 pointVelocity)
    {
        Vector3 springForce = lassoHoldStrength * displacement;
        float damping = 2f * Mathf.Sqrt(lassoHoldStrength * SnaredObject.Rb.mass);
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
            Vector3 toPlayer = (transform.position - worldFaceNormal).normalized;
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

    #region Anchor Adjustment

    public void MoveAnchorPointZ(float scrollInput)
    {
        if (Mathf.Approximately(scrollInput, 0f)) return;

        //AnchorDist += Mathf.Sign(scrollInput) * reelIncrement;
        //AnchorDist = Mathf.Round(AnchorDist / reelIncrement) * reelIncrement;
        AnchorDist += scrollInput;
        AnchorDist = Mathf.Clamp(AnchorDist, minLassoRange, MaxLassoRange);
    }

    public void MoveAnchorPointY(float lookInputY)
    {
        if (SuppressLiftInput) return;
        if (Mathf.Approximately(lookInputY, 0f)) return;

        CurrentLiftOffset += lookInputY * liftSpeed * Time.deltaTime;
    }

    public void SetVerticalAnchor(bool setMinimum)
    {
        Vector3 baseTargetPos = GetBaseTargetPos();

        float characterFeetY = transform.position.y - 1f;
        float absoluteMaxY = characterFeetY + maxLiftHeight;
        float absoluteMinY = GetMinimumLiftHeight();

        float maxAllowedOffset = absoluteMaxY - baseTargetPos.y;
        float minAllowedOffset = absoluteMinY - baseTargetPos.y;

        if (setMinimum) CurrentLiftOffset = minAllowedOffset;

        CurrentLiftOffset = Mathf.Clamp(CurrentLiftOffset, minAllowedOffset, maxAllowedOffset);
    }
    public void AddLiftOffset(float delta)
    {
        CurrentLiftOffset += delta;
    }

    public float GetMinimumLiftHeight()
    {
        if (SnaredObject == null || SnaredObject.transform == null)
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

        if (!float.IsNegativeInfinity(groundYBelowObject))
        {
            return groundYBelowObject + objectHalfHeight + minHeightAboveGround;
        }

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

    #region Swing Setup

    public void HandleSwingSetup()
    {
        _attachPointLocal = SnaredObject.transform.InverseTransformPoint(GetNearestSwingPoint().position);
        _playerRefData.PlayerSwing.StartSwing(HitPos, _playerRefData.PlayerMovement.Rb.linearVelocity, AnchorDist);
    }

    private Transform GetNearestSwingPoint()
    {
        Transform nearestSwingPoint = null;
        float currentNearestDist = float.MaxValue;

        foreach (Transform child in SnaredObject.transform)
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

        if (nearestSwingPoint == null && SnaredObject.transform.TryGetComponent(out SwingPoint parentSwingPoint))
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

        SnaredObject.OnPropDestroyed -= HandleObjectReleased;
        SnaredObject.ActivateOutline(false);
        SnaredObject.OnRelease();

        SnaredObject.transform.gameObject.tag = SnaredObject.OriginalTag;
        if (SnaredObject.TryGetComponent(out PluckOutProp po)) po.ResetLassoVariables();
        SnaredObject = null;

        CurrentLiftOffset = 0f;
        _localFaceNormal = Vector3.zero;

        lassoGrabVisualIndicator.SetActive(false);
        OnLassoReleased?.Invoke();
    }

    public void HandleHold()
    {
        if (SnaredObject == null) return;

        SnaredObject.ActivateOutline(false);
        SnaredObject = null;
        _localFaceNormal = Vector3.zero;
        lassoGrabVisualIndicator.SetActive(false);
    }

    #endregion

}