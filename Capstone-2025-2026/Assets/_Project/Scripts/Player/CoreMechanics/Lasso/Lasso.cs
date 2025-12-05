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
    [SerializeField] private float throwStrength = 25f;
    [SerializeField, Range(0, 1)] private float lookAtStrength = 0.5f;
    [SerializeField, Range(0, 0.2f)] private float lookAtDamping = 0.05f;
    [SerializeField] private float rotationalDampingStrength = 0.5f;
    [SerializeField] private float maxLassoStrength;
    [SerializeField] private bool useSizeScale;

    [Header("Aim Assist Properties")]
    [SerializeField] private AimAssistType aimAssistType;
    [SerializeField] private float aimAssistBufferRadius = 1.5f;
    [SerializeField] private bool useAimOutline = true;
    [SerializeField] private bool usePickupOutline = true;
    [SerializeField] private bool useGrabPointsForHold = false;
    [SerializeField] private bool usePhysicsTorque = true;

    [Header("Swinging")]
    [SerializeField] private float swingJumpForce = 5f;
    [SerializeField] private Transform forwardRef;

    [Header("Internal Variables")]
    private AimAssist _aimAssist;
    private PlayerSwing _swingController;
    private Transform _snaredObjTransform;
    private Transform _nearestGrabPoint;
    private Vector3 _attachPointLocal;
    private Vector3 _localFaceNormal;
    private Joint swingJoint;

    [Header("Getters")]
    public Prop SnaredObject { get; private set; }
    public PlayerMovement PlayerController { get; private set; }
    public Vector3 HitPos => _snaredObjTransform.transform.TransformPoint(_attachPointLocal);
    public float AnchorDist { get; private set; }

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
        if (_snaredObjTransform == null || SnaredObject == null) // destroyed
        {
            HandleObjectReleased();
        }

        CheckNearbyTargets();
    }

    #endregion

    #region Helper Functions

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
        Vector3 maxDistancePos = PlayerCamLookPos.position + camOffset + ray.direction * AnchorDist;

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

            _snaredObjTransform = prop.transform;
            SnaredObject = prop;
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
            SnaredObject.Rb.AddTorque(totalTorque, ForceMode.Force); //temp (?)
        }

        SnaredObject.Rb.angularVelocity *= 0.975f; //stop excessive spin
    }

    private Vector3 CalculateLinearForce(Vector3 displacement, Vector3 pointVelocity)
    {
        //linear force
        Vector3 springForce = centerStrength * displacement; //F = -springRate * displacement
        float damping = 2f * Mathf.Sqrt(centerStrength * SnaredObject.Rb.mass); //critical damping = 2 * sqrt(springRate * mass)
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

        if (SnaredObject.IsTouchingSurface) return (correctiveTorque * scale) + dampingTorque;
        else return correctiveTorque * scale;

    }

    private Vector3 CalculateLookAtTorque()
    {
        Vector3 lookAtSpringTorque = Vector3.zero;
        Vector3 lookAtDampingTorque = Vector3.zero;
        if (_localFaceNormal != Vector3.zero)
        {
            Vector3 worldFaceNormal = SnaredObject.transform.TransformDirection(_localFaceNormal);
            Vector3 toPlayer = (PlayerCamLookPos.position - SnaredObject.Rb.worldCenterOfMass).normalized;
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

    /*public void MoveObjectToPos(Vector3 desiredPos)
    {
        if (SnaredObject == null) return;

        float currentDist = Vector3.Distance(desiredPos, HitPos);
        float currentSpeed = Mathf.SmoothStep(0f, centerStrength, currentDist / 5f) * Time.fixedDeltaTime;
        Vector3 direction = desiredPos - HitPos;
        SnaredObject.Rb.linearVelocity = direction.normalized * currentSpeed;
        SnaredObject.Rb.angularVelocity *= 0.99f;
    }*/

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
        PlayerController.Rb.AddForce((Vector3.up + forwardRef.forward).normalized * swingJumpForce, ForceMode.Impulse);
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

        SnaredObject.OnPropDestroyed -= HandleObjectReleased;
        SnaredObject.ActivateOutline(false);
        SnaredObject.OnRelease();
        SnaredObject = null;
        _snaredObjTransform = null;
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

    #region Anchoring

    public void AnchorToObject()
    {
        _swingController.UpdateAnchorPoint(HitPos);
        _swingController.ConstrainToRope();
    }

    public void HandleAnchorStart()
    {
        _swingController.SetRopeLength(maxLassoRange * 1.1f);
    }

    #endregion
}
