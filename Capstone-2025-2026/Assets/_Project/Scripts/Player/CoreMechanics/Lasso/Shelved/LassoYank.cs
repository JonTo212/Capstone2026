using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LassoYank : MonoBehaviour
{
    [Header("Object Yank Properties")]
    [SerializeField] private float handAttachThreshold = 0.2f;
    [SerializeField] private float objectYankDuration = 0.5f;
    [SerializeField] private float consideredStuckVel = 0.1f;
    [SerializeField] private float equalWeightYankForce = 15f;

    [Header("Player Yank Properties")]
    [SerializeField] private float playerYankStopBuffer = 0.3f;

    [Header("Snapback")]
    [SerializeField] private float maxStretchDist = 1f;
    [SerializeField] private AnimationCurve elasticCurve;
    [SerializeField] private float snapbackImpulseStrength = 20f;
    [SerializeField] private Volume tempVignetteVolume;

    private Lasso playerLasso;
    private Coroutine _objectYankCoroutine;
    private Coroutine _playerYankCoroutine;
    private bool _isStrainingAtMaxDistance = false;

    public event Action OnObjectYankCompleted;
    public event Action OnPlayerYankCompleted;


    #region Unity Functions
    private void Awake()
    {
        playerLasso = GetComponent<Lasso>();
    }
    #endregion

    #region Helper Functions

    private Vector3 CalculatePlayerYankVelocity(Vector3 start, Vector3 end, float peakHeight, out float flightTime) //no control over duration (ballistic trajectory)
    {
        //v = sqrt(-2 * gravity * height)
        float gravity = -playerLasso.PlayerController.Gravity;
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


    #endregion

    #region Object Yank
    public void HandleObjectYank()
    {
        if (playerLasso.SnaredObject == null) return;

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
        playerLasso.SnaredObject.Rb.useGravity = true;
        playerLasso.SnaredObject.Rb.linearDamping = 0f;

        Vector3 startPosition = playerLasso.SnaredObject.transform.position;
        Vector3 toPlayer = playerLasso.HoldPos.position - startPosition;
        float startTime = Time.time;

        bool shouldSkipYankLoop = playerLasso.SnaredObject.Rb.mass == playerLasso.PlayerController.Rb.mass;
        if (shouldSkipYankLoop)
        {
            playerLasso.SnaredObject.Rb.AddForceAtPosition(equalWeightYankForce * toPlayer.normalized, playerLasso.HitPos, ForceMode.Impulse);
            playerLasso.HandleObjectReleased();

            if (tempVignetteVolume.profile.TryGet<Vignette>(out var _vignette))
            {
                _vignette.intensity.value = 0f;
            }
        }
        else
        {
            //apply initial velocity
            //AudioManager.Instance.PlaySFX(AudioManager.Instance.Pull, 5, 1);
            Vector3 startVel = CalculateObjectYankVelocity(startPosition, playerLasso.HoldPos.position, objectYankDuration);
            playerLasso.SnaredObject.Rb.linearVelocity = Vector3.zero;
            playerLasso.SnaredObject.Rb.AddForce(startVel * playerLasso.SnaredObject.Rb.mass, ForceMode.Impulse);

            Vector3 previousPos = playerLasso.SnaredObject.transform.position;
            float stuckTimer = 0;
            float stuckTime = 0.4f;

            while (Vector3.Distance(playerLasso.SnaredObject.transform.position, playerLasso.HoldPos.position) > handAttachThreshold)
            {
                if (playerLasso.SnaredObject == null) break;

                //snapback if object is stuck for too long
                float displacement = (playerLasso.SnaredObject.transform.position - previousPos).magnitude;
                previousPos = playerLasso.SnaredObject.transform.position;

                if (displacement < consideredStuckVel)
                    stuckTimer += Time.fixedDeltaTime;
                else
                    stuckTimer = 0f;

                if (stuckTimer >= stuckTime)
                {
                    ApplySnapbackForce(1f);
                    playerLasso.HandleObjectReleased();
                    break;
                }

                //calculate correctional pull velocity
                float elapsedTime = Time.time - startTime;
                float remainingTime = objectYankDuration - elapsedTime;
                float clampedRemainingTime = Mathf.Max(remainingTime, 0.05f);

                Vector3 idealVelocity = CalculateObjectYankVelocity(playerLasso.SnaredObject.transform.position, playerLasso.HoldPos.position, clampedRemainingTime);
                Vector3 velocityError = idealVelocity - playerLasso.SnaredObject.Rb.linearVelocity;

                playerLasso.SnaredObject.Rb.AddForce(velocityError * playerLasso.SnaredObject.Rb.mass, ForceMode.Impulse);


                //calculate correctional torque
                Quaternion targetRotation = Quaternion.LookRotation(playerLasso.HoldPos.forward, Vector3.up);
                Quaternion deltaRotation = targetRotation * Quaternion.Inverse(playerLasso.SnaredObject.transform.rotation);

                deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
                if (angle > 180f) angle -= 360f;

                Vector3 angularVelocity = axis * angle * Mathf.Deg2Rad / clampedRemainingTime;
                Vector3 angularError = angularVelocity - playerLasso.SnaredObject.Rb.angularVelocity;

                playerLasso.SnaredObject.Rb.AddTorque(angularError, ForceMode.VelocityChange);


                yield return new WaitForFixedUpdate();
            }

            if (playerLasso.SnaredObject != null)
            {
                playerLasso.SnaredObject.OnHold(playerLasso.HoldPos);
                playerLasso.SnaredObject.AttachedTransform = transform;
            }
        }

        OnObjectYankCompleted?.Invoke();
        _objectYankCoroutine = null;
    }

    #endregion

    #region Player Yank
    public void HandlePlayerYank()
    {
        if (playerLasso.SnaredObject == null) return;

        if (_playerYankCoroutine != null)
        {
            StopCoroutine(_playerYankCoroutine);
        }


        //AudioManager.Instance.PlaySFX(AudioManager.Instance.Pull, 5, 1);
        _playerYankCoroutine = StartCoroutine(YankPlayerCoroutine());
    }


    public IEnumerator YankPlayerCoroutine()
    {
        Collider objCol = playerLasso.SnaredObject.GetComponent<Collider>();
        CapsuleCollider playerCol = playerLasso.PlayerController.GetComponent<CapsuleCollider>();

        float tolerance = 0.2f;
        Vector3 lowestPoint = new Vector3(transform.position.x, transform.position.y - (playerCol.height / 2), transform.position.z);
        Vector3 aboveObj = playerLasso.SnaredObject.transform.position + Vector3.up * (objCol.bounds.extents.y + playerCol.height / 2 + tolerance);

        float grapplePointRelativeYPos = playerLasso.SnaredObject.transform.position.y - lowestPoint.y;
        float highestPointOnArc = grapplePointRelativeYPos + 2f;
        if (grapplePointRelativeYPos < 0) highestPointOnArc = 2f;

        //compute initial velocity and expected flight time for the planned arc
        Vector3 startPos = playerLasso.PlayerController.transform.position;
        Vector3 initialVel = CalculatePlayerYankVelocity(startPos, aboveObj, highestPointOnArc, out float flightTime);
        Vector3 gravityVec = Vector3.up * -playerLasso.PlayerController.Gravity;

        playerLasso.PlayerController.Rb.linearVelocity = initialVel;
        float launchTime = Time.time;

        while (Vector3.Distance(aboveObj, playerCol.ClosestPoint(aboveObj)) > playerYankStopBuffer)
        {
            if (playerLasso.SnaredObject == null) break;

            float elapsed = Time.time - launchTime;
            float t = Mathf.Clamp(elapsed, 0f, flightTime);

            //apply correction force based on arc calculation
            Vector3 plannedPos = startPos + initialVel * t + 0.5f * gravityVec * t * t;
            Vector3 plannedVel = initialVel + gravityVec * t;
            Vector3 velError = plannedVel - playerLasso.PlayerController.Rb.linearVelocity;

            playerLasso.PlayerController.Rb.AddForce(velError, ForceMode.Impulse);

            yield return new WaitForFixedUpdate();
        }

        if (playerLasso.SnaredObject != null)
        {
            playerLasso.PlayerController.Rb.linearVelocity = Vector3.zero; //only do this when you land on top
        }

        OnPlayerYankCompleted?.Invoke();
        _playerYankCoroutine = null;
    }

    #endregion

    #region Snapback
    public void HandleSnapback()
    {
        if (playerLasso.SnaredObject == null) return;

        Vector3 dirToPlayer = playerLasso.HoldPos.position - playerLasso.HitPos;
        Vector3 pullDirection = dirToPlayer.normalized;
        float currentDistance = dirToPlayer.magnitude;
        float maxDistance = playerLasso.AnchorDist + maxStretchDist;
        float stretchDistance = currentDistance - playerLasso.AnchorDist;
        float accelMagnitudeAway = Vector3.Dot(playerLasso.PlayerController.Rb.linearVelocity, pullDirection);
        float normalizedStretch = stretchDistance / maxDistance;
        float tensionFactor = elasticCurve.Evaluate(normalizedStretch);
        Vector3 counterForce = -pullDirection * accelMagnitudeAway * tensionFactor;

        if (tempVignetteVolume.profile.TryGet<Vignette>(out var _vignette))
        {
            _vignette.intensity.value = tensionFactor / 3f;
            if (tensionFactor > 0.5f)
            {
                //AudioManager.Instance.PlaySFX(AudioManager.Instance.TetherTighten, 5, 1);
            }
        }

        if (currentDistance > playerLasso.AnchorDist)
        {
            if (accelMagnitudeAway > 0)
            {
                playerLasso.PlayerController.Rb.AddForce(counterForce / Time.fixedDeltaTime, ForceMode.Acceleration);
            }

            if (currentDistance >= maxDistance)
            {
                if (playerLasso.PlayerController.WishDir.sqrMagnitude > 0.01f)
                {
                    _isStrainingAtMaxDistance = true;
                }
                else if (_isStrainingAtMaxDistance)
                {
                    ApplySnapbackForce(tensionFactor);
                    _isStrainingAtMaxDistance = false;
                    _vignette.intensity.value = 0f;
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
        Vector3 dirToPlayer = playerLasso.HoldPos.position - playerLasso.HitPos;
        Vector3 pullDirection = dirToPlayer.normalized;
        playerLasso.PlayerController.Rb.AddForce(-pullDirection * snapbackImpulseStrength * impulseMultiplier, ForceMode.Impulse);

        //AudioManager.Instance.PlaySFX(AudioManager.Instance.Pull, 5, 1);
    }

    #endregion

}
