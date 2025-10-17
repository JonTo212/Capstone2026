using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Lasso : MonoBehaviour
{
    [Header("External Components")]
    [field: SerializeField] public Transform HoldPos { get; private set; }
    [field: SerializeField] public Camera PlayerCam { get; private set; }

    [Header("Lasso Properties")]
    [SerializeField] private float lassoRange = 25f;
    [SerializeField] private float centerStrength = 250f;
    [SerializeField] private float throwStrength = 25f;

    [Header("Aim Assist Properties")]
    [SerializeField] private AimAssistType aimAssistType;
    [SerializeField] private float aimAssistBufferRadius = 1.5f;
    [SerializeField] private bool useAimOutline = true;
    [SerializeField] private bool usePickupOutline = true;

    [Header("Object Yank Properties")]
    [SerializeField] private float handAttachThreshold = 0.2f;
    [SerializeField] private float objectYankDuration = 0.5f;
    [SerializeField] private float consideredStuckVel = 0.1f;
    [SerializeField] private float equalWeightYankForce = 5f;
    [SerializeField] private float velocityDistanceThreshold = 2f;

    [Header("Player Yank Properties")]
    [SerializeField] private float playerYankStopBuffer = 0.3f;

    [Header("Snapback")]
    [SerializeField] private float maxStretchDist = 1f;
    [SerializeField] private AnimationCurve elasticCurve;
    [SerializeField] private float snapbackImpulseStrength = 20f;
    [SerializeField] private Volume tempVignetteVolume;

    [Header("Swinging")]
    [SerializeField] private float springRate = 10f;
    [SerializeField] private float swingJumpForce = 5f;

    [Header("Internal Variables")]
    private AimAssist _aimAssist;
    private PlayerMovement _playerController;
    private Coroutine _objectYankCoroutine;
    private Coroutine _playerYankCoroutine;
    private Transform _snaredObjTransform;
    private Vector3 _attachPointLocal;
    private Joint swingJoint;
    private bool _isStrainingAtMaxDistance = false;
    private float _anchorDist;
    private float _currentAnchorDist;

    [Header("Getters")]
    public Prop SnaredObject { get; private set; }
    public Vector3 HitPos => _snaredObjTransform.transform.TransformPoint(_attachPointLocal);

    public event Action OnObjectHit;
    public event Action OnLassoReleased;
    public event Action OnObjectYankCompleted;
    public event Action OnPlayerYankCompleted;
    public AudioManager aManage;


    #region Unity Functions
    private void Awake()
    {
        _playerController = GetComponent<PlayerMovement>();
        _aimAssist = new AimAssist();
        
        aManage = GameObject.Find("AudioManager").GetComponent<AudioManager>();
    }

    private void Update()
    {
        if (_snaredObjTransform == null) // destroyed
        {
            HandleObjectReleased();
        }

        CheckNearbyTargets();
    }

    #endregion

    #region Helper Functions

    public Vector3 GetCenterOfScreen()
    {
        Ray ray = PlayerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        float checkDist = _anchorDist;
        float maxCheckDist = lassoRange;

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, checkDist) && hit.transform != _snaredObjTransform)
        {
            checkDist = hit.distance + 0.1f;
        }

        _currentAnchorDist = checkDist;

        return ray.origin + ray.direction * _currentAnchorDist;
    }

    public Vector3 GetAnchoredCenterOfScreen()
    {
        Ray ray = PlayerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 maxDistancePos = ray.origin + ray.direction * _anchorDist;
        return maxDistancePos;
    }

    private Vector3 CalculatePlayerYankVelocity(Vector3 start, Vector3 end, float peakHeight, out float flightTime) //no control over duration (ballistic trajectory)
    {
        //v = sqrt(-2 * gravity * height)
        float gravity = -_playerController.Gravity;
        float displacementY = end.y - start.y;
        float height = Mathf.Max(peakHeight, displacementY + 0.1f); //ensure there's no zero

        float timeUp = Mathf.Sqrt(-2f * height / gravity);
        float timeDown = Mathf.Sqrt(2f * (displacementY - height) / gravity);
        flightTime = timeUp + timeDown;

        Vector3 displacementXZ = new Vector3(end.x - start.x, 0f, end.z - start.z);

        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2f * gravity * height);
        Vector3 velocityXZ = displacementXZ / flightTime;

        return velocityXZ + velocityY;
    }

    private Vector3 CalculateObjectYankVelocity(Vector3 start, Vector3 end, float flightTime) //no control over Y (projectile motion)
    {
        //initial vel = (total displacement - (1/2(accel * time)^2)) / time)
        Vector3 displacement = end - start;
        float gravity = Physics.gravity.y;

        Vector3 velocityXZ = new Vector3(displacement.x / flightTime, 0f, displacement.z / flightTime);
        float velocityY = (displacement.y - 0.5f * gravity * flightTime * flightTime) / flightTime;

        return velocityXZ + Vector3.up * velocityY;
    }

    private void CheckNearbyTargets()
    {
        Prop targetProp = null;

        if (useAimOutline)
        {
            RaycastHit? hit = _aimAssist.GetAssistHitPoint(PlayerCam, PlayerCam.transform.position, lassoRange, aimAssistType, aimAssistBufferRadius);
            if (hit.HasValue)
            {
                targetProp = hit.Value.transform.GetComponentInParent<Prop>();
            }
        }

        _aimAssist.HighlightSelectedProp(targetProp, SnaredObject != null);
    }


    #endregion

    #region Start Lasso
    public void HandleLassoStart()
    {
        RaycastHit? hit = _aimAssist.GetAssistHitPoint(PlayerCam, PlayerCam.transform.position, lassoRange, aimAssistType, aimAssistBufferRadius);
        if (hit.HasValue)
        {
            RaycastHit actualHit = hit.Value;
            Ray noAssistRay = PlayerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Prop prop = actualHit.transform.GetComponentInParent<Prop>();

            _anchorDist = Vector3.Distance(actualHit.point, noAssistRay.origin);
            _attachPointLocal = prop.transform.InverseTransformPoint(actualHit.point);
            _snaredObjTransform = prop.transform;

            SnaredObject = prop;
            prop.OnSnare();
            prop.OnPropDestroyed += HandleObjectReleased;
            if (usePickupOutline) SnaredObject.ActivateOutline(true);

            if (prop.TryGetComponent(out IActivatable activatable))
            {
                activatable.Activate();
            }

            OnObjectHit?.Invoke();
            aManage.PlaySFX(aManage.Thrown, 5, 1);
        }
    }

    #endregion

    #region Hold Object At Center
    public void HandleObjectHoldAtDistance(Vector3 desiredPos)
    {
        if (SnaredObject == null) return;
         
        Vector3 dirToHoldPos = desiredPos - HitPos;
        SnaredObject.Rb.AddForceAtPosition(dirToHoldPos * centerStrength, HitPos, ForceMode.Force);
    }

    public void RotateHeldObject()
    {
        Quaternion targetRotation = Quaternion.LookRotation(HoldPos.forward, Vector3.up);
        SnaredObject.Rb.MoveRotation(targetRotation);
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

    public void MoveObjectToPos(Vector3 desiredPos)
    {
        Vector3 attachPointWorld = SnaredObject.transform.TransformPoint(_attachPointLocal);
        Vector3 pointVelocity = SnaredObject.Rb.GetPointVelocity(attachPointWorld);
        Vector3 displacement = desiredPos - attachPointWorld;


        //linear force
        Vector3 springForce = centerStrength * displacement; //F = -springRate * displacement
        float damping = 2f * Mathf.Sqrt(centerStrength * SnaredObject.Rb.mass); //critical damping = 2 * sqrt(springRate * mass)
        Vector3 dampingForce = -pointVelocity * damping;
        Vector3 totalForce = springForce + dampingForce;


        //torque
        Vector3 r = attachPointWorld - SnaredObject.Rb.worldCenterOfMass;
        float leverArmLength = (attachPointWorld - SnaredObject.Rb.worldCenterOfMass).magnitude;
        float scale = 1f / (1f + leverArmLength);
        Vector3 torque = Vector3.Cross(r, springForce + dampingForce);
        Vector3 scaledTorque = torque * scale;


        //SnaredObject.Rb.AddForceAtPosition(springForce + dampingForce, attachPointWorld, ForceMode.Acceleration); //accel works because the damping already takes into account mass
        SnaredObject.Rb.AddForce(totalForce, ForceMode.Acceleration);
        SnaredObject.Rb.AddTorque(scaledTorque, ForceMode.Acceleration);
        SnaredObject.Rb.angularVelocity *= 0.99f; //stop excessive spin
    }
    #endregion

    #region Snapback
    public void HandleSnapback()
    {
        if (SnaredObject == null) return;

        Vector3 dirToPlayer = HoldPos.position - HitPos;
        Vector3 pullDirection = dirToPlayer.normalized;
        float currentDistance = dirToPlayer.magnitude;
        float maxDistance = _anchorDist + maxStretchDist;
        float stretchDistance = currentDistance - _anchorDist;
        float accelMagnitudeAway = Vector3.Dot(_playerController.Rb.linearVelocity, pullDirection);
        float normalizedStretch = stretchDistance / maxDistance;
        float tensionFactor = elasticCurve.Evaluate(normalizedStretch);
        Vector3 counterForce = -pullDirection * accelMagnitudeAway * tensionFactor;

        if (tempVignetteVolume.profile.TryGet<Vignette>(out var _vignette))
        {
            _vignette.intensity.value = tensionFactor / 3f;
            if (tensionFactor > 0.5f && !aManage.SFXSource5.isPlaying)
            {
                aManage.PlaySFX(aManage.Pull, 5, 1);
            }
        }

        if (currentDistance > _anchorDist)
        {
            if (accelMagnitudeAway > 0)
            {
                _playerController.Rb.AddForce(counterForce / Time.fixedDeltaTime, ForceMode.Acceleration);
            }

            if (currentDistance >= maxDistance)
            {
                if (_playerController.WishDir.sqrMagnitude > 0.01f)
                {
                    _isStrainingAtMaxDistance = true;
                }
                else if (_isStrainingAtMaxDistance)
                {
                    ApplySnapbackForce(tensionFactor);
                    _isStrainingAtMaxDistance = false;
                }
            }
        }
        else
        {
            _isStrainingAtMaxDistance = false;
        }
    }

    public void ApplySnapbackForce(float impulseMultiplier)
    {
        Vector3 dirToPlayer = HoldPos.position - HitPos;
        Vector3 pullDirection = dirToPlayer.normalized;
        _playerController.Rb.AddForce(-pullDirection * snapbackImpulseStrength * impulseMultiplier, ForceMode.Impulse);
    }

    #endregion

    #region Swinging

    public void HandleSwingSetup()
    {
        SpringJoint joint = gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = HitPos;

        float currentDist = Vector3.Distance(HoldPos.position, HitPos);
        joint.maxDistance = _anchorDist * 0.85f;
        joint.minDistance = _anchorDist * 0.15f;

        joint.spring = springRate;
        float damping = 2f * Mathf.Sqrt(joint.spring * _playerController.Rb.mass);
        joint.damper = damping;
        joint.massScale = 4.5f;

        swingJoint = joint;
    }

    public void SwingJumpBoost()
    {
        HandleObjectReleased();
        _playerController.Rb.AddForce(Vector3.up * swingJumpForce, ForceMode.Impulse);
    }

    #endregion

    #region Object Yank
    public void HandleObjectYank()
    {
        if (SnaredObject == null) return;

        if (_objectYankCoroutine != null)
        {
            StopCoroutine(_objectYankCoroutine);
        }

        _objectYankCoroutine = StartCoroutine(YankObjectCoroutine());
    }

    /*private IEnumerator YankObjectWithForce()
    {
        SnaredObject.Rb.useGravity = true;
        SnaredObject.Rb.linearDamping = 0f;

        bool shouldSkipYankLoop = SnaredObject.Rb.mass == _playerController.Rb.mass;
        if (shouldSkipYankLoop)
        {
            Vector3 startPosition = SnaredObject.transform.position;
            Vector3 endPosition = HoldPos.position;
            Vector3 toPlayer = endPosition - startPosition;
            SnaredObject.Rb.AddForceAtPosition(equalWeightYankForce * toPlayer.normalized, HitPos, ForceMode.Impulse);
            HandleObjectReleased();
        }
        else
        {
            while (Vector3.Distance(SnaredObject.transform.position, HoldPos.position) > handAttachThreshold)
            {
                if (SnaredObject == null) break;

                Vector3 startPosition = SnaredObject.transform.position;
                Vector3 endPosition = HoldPos.position;
                Vector3 toPlayer = endPosition - startPosition;

                if (SnaredObject.Rb.linearVelocity.magnitude < maxObjectVelocity && toPlayer.magnitude > velocityDistanceThreshold)
                {
                    SnaredObject.Rb.AddForce(pullForce * toPlayer.normalized, ForceMode.Force);
                }
                else
                {
                    SnaredObject.Rb.linearVelocity = toPlayer.normalized * maxObjectVelocity;
                }

                yield return new WaitForFixedUpdate();
            }
            if (SnaredObject != null)
            {
                SnaredObject.OnHold(HoldPos);
                SnaredObject.AttachedTransform = transform;
            }
        }

        OnObjectYankCompleted?.Invoke();
        _objectYankCoroutine = null;
    }*/

    private IEnumerator YankObjectCoroutine()
    {
        SnaredObject.Rb.useGravity = true;
        SnaredObject.Rb.linearDamping = 0f;

        Vector3 startPosition = SnaredObject.transform.position;
        Vector3 toPlayer = HoldPos.position - startPosition;
        float startTime = Time.time;

        bool shouldSkipYankLoop = SnaredObject.Rb.mass == _playerController.Rb.mass;
        if (shouldSkipYankLoop)
        {
            SnaredObject.Rb.AddForceAtPosition(equalWeightYankForce * toPlayer.normalized, HitPos, ForceMode.Impulse);
            HandleObjectReleased();
        }
        else
        {
            //apply initial velocity
            aManage.PlaySFX(aManage.Pull, 5, 1);
            Vector3 startVel = CalculateObjectYankVelocity(startPosition, HoldPos.position, objectYankDuration);
            SnaredObject.Rb.linearVelocity = Vector3.zero;
            SnaredObject.Rb.AddForce(startVel * SnaredObject.Rb.mass, ForceMode.Impulse);

            Vector3 previousPos = SnaredObject.transform.position;
            float stuckTimer = 0;
            float stuckTime = 0.4f;

            while (Vector3.Distance(SnaredObject.transform.position, HoldPos.position) > handAttachThreshold)
            {
                if (SnaredObject == null) break;

                //snapback if object is stuck for too long
                float displacement = (SnaredObject.transform.position - previousPos).magnitude;
                previousPos = SnaredObject.transform.position;

                if (displacement < consideredStuckVel)
                    stuckTimer += Time.fixedDeltaTime;
                else
                    stuckTimer = 0f;

                if (stuckTimer >= stuckTime)
                {
                    ApplySnapbackForce(1f);
                    HandleObjectReleased();
                    break;
                }

                //calculate correctional pull velocity
                float elapsedTime = Time.time - startTime;
                float remainingTime = objectYankDuration - elapsedTime;
                float clampedRemainingTime = Mathf.Max(remainingTime, 0.05f);

                Vector3 idealVelocity = CalculateObjectYankVelocity(SnaredObject.transform.position, HoldPos.position, clampedRemainingTime);
                Vector3 velocityError = idealVelocity - SnaredObject.Rb.linearVelocity;

                SnaredObject.Rb.AddForce(velocityError * SnaredObject.Rb.mass, ForceMode.Impulse);


                //calculate correctional torque
                Quaternion targetRotation = Quaternion.LookRotation(HoldPos.forward, Vector3.up);
                Quaternion deltaRotation = targetRotation * Quaternion.Inverse(SnaredObject.transform.rotation);

                deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
                if (angle > 180f) angle -= 360f;

                Vector3 angularVelocity = axis * angle * Mathf.Deg2Rad / clampedRemainingTime;
                Vector3 angularError = angularVelocity - SnaredObject.Rb.angularVelocity;

                SnaredObject.Rb.AddTorque(angularError, ForceMode.VelocityChange);


                yield return new WaitForFixedUpdate();
            }

            if (SnaredObject != null)
            {
                SnaredObject.OnHold(HoldPos);
                SnaredObject.AttachedTransform = transform;
            }
        }

        OnObjectYankCompleted?.Invoke();
        _objectYankCoroutine = null;
    }

    #endregion

    #region Player Yank
    public void HandlePlayerYank()
    {
        if (SnaredObject == null) return;

        if (_playerYankCoroutine != null)
        {
            StopCoroutine(_playerYankCoroutine);
        }

        _playerYankCoroutine = StartCoroutine(YankPlayerCoroutine());
    }


    public IEnumerator YankPlayerCoroutine()
    {
        Collider objCol = SnaredObject.GetComponent<Collider>();
        CapsuleCollider playerCol = _playerController.GetComponent<CapsuleCollider>();

        float tolerance = 0.2f;
        Vector3 lowestPoint = new Vector3(transform.position.x, transform.position.y - (playerCol.height / 2), transform.position.z);
        Vector3 aboveObj = SnaredObject.transform.position + Vector3.up * (objCol.bounds.extents.y + playerCol.height / 2 + tolerance);

        float grapplePointRelativeYPos = SnaredObject.transform.position.y - lowestPoint.y;
        float highestPointOnArc = grapplePointRelativeYPos + 2f;
        if (grapplePointRelativeYPos < 0) highestPointOnArc = 2f;

        //compute initial velocity and expected flight time for the planned arc
        Vector3 startPos = _playerController.transform.position;
        Vector3 initialVel = CalculatePlayerYankVelocity(startPos, aboveObj, highestPointOnArc, out float flightTime);
        Vector3 gravityVec = Vector3.up * -_playerController.Gravity;

        _playerController.Rb.linearVelocity = initialVel;
        float launchTime = Time.time;

        while (Vector3.Distance(aboveObj, playerCol.ClosestPoint(aboveObj)) > playerYankStopBuffer)
        {
            if (SnaredObject == null) break;

            float elapsed = Time.time - launchTime;
            float t = Mathf.Clamp(elapsed, 0f, flightTime);

            //apply correction force based on arc calculation
            Vector3 plannedPos = startPos + initialVel * t + 0.5f * gravityVec * t * t;
            Vector3 plannedVel = initialVel + gravityVec * t;
            Vector3 velError = plannedVel - _playerController.Rb.linearVelocity;

            _playerController.Rb.AddForce(velError, ForceMode.Impulse);

            yield return new WaitForFixedUpdate();
        }

        if (SnaredObject != null)
        {
            _playerController.Rb.linearVelocity = Vector3.zero; //only do this when you land on top
        }

        aManage.PlaySFX(aManage.Pull, 5, 1);

        OnPlayerYankCompleted?.Invoke();
        _playerYankCoroutine = null;
    }

    #endregion

    #region Throw / Release
    public void HandleObjectThrow()
    {
        if (SnaredObject == null) return;

        Ray ray = PlayerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        SnaredObject.OnThrow(ray.direction, throwStrength);
        aManage.PlaySFX(aManage.Thrown, 5, 1);
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

        OnLassoReleased?.Invoke();
    }
    #endregion
}
