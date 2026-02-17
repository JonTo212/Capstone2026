using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraCutsceneHandler : MonoBehaviour
{
    public static CameraCutsceneHandler Instance { get; private set; }

    [Header("References")]
    [SerializeField] private PlayerActions input;
    [SerializeField] private Rigidbody playerRb;
    [SerializeField] private PlayerMovement playerController;
    [SerializeField] private PlayerModelRotationHandler playerModelRotation;
    [SerializeField] private ZeldaCameraController cameraController;
    [SerializeField] private CameraModeController cameraModeController;

    [Header("Transition Settings")]
    [SerializeField] private float blendInTime = 0.5f;
    [SerializeField] private float blendInDelay = 0.5f;

    [Header("Camera Settings")]
    [Tooltip("Lock player input to camera rotation during the swing")]
    [SerializeField] private bool lockCameraInput = true;

    [Tooltip("Camera will automatically rotate to face the direction of movement")]
    [SerializeField] private bool autoRotateCamera = true;

    [Tooltip("How quickly the camera rotates to face movement direction")]
    [SerializeField] private float cameraRotationSpeed = 3f;

    [Tooltip("Pitch angle for the camera (negative looks down, positive looks up)")]
    [SerializeField] private float cameraPitch = 10f;

    [Tooltip("Apply RopeHangCutscene camera state during swing")]
    [SerializeField] private bool useCustomCameraState = true;

    [Header("Player Animation")]
    [Tooltip("Make the player sway/tilt during the swing")]
    [SerializeField] private bool enablePlayerSway = true;

    [Tooltip("Maximum tilt angle when swaying")]
    [SerializeField] private float maxSwayAngle = 15f;

    [Tooltip("Time in seconds to reach the maximum sway angle")]
    [SerializeField] private float durationUntilMaxAngle = 0.3f;

    [Tooltip("Maximum forward pitch angle when moving along the path")]
    [SerializeField] private float maxLeanAngle = 20f;

    [Tooltip("Time in seconds to build up to the maximum forward lean")]
    [SerializeField] private float durationUntilMaxLean = 0.4f;

    private bool _isActive = false;
    private bool _isPlaying = false;
    public bool IsCutsceneActive => _isPlaying;

    // Path data
    private float _startTime;
    private float _duration;
    private List<Transform> _playerPath;
    private List<Vector3> _splinePath;
    private bool _useSplinePath = false;

    // Blend state
    private bool _isBlendingIn = false;
    private float _blendStartTime;
    private float _blendInDelayTimer;
    private Vector3 _blendPlayerFrom;
    private Vector3 _blendPlayerTo;
    public bool BlendingIn => _isBlendingIn;
    public bool BlendDelayActive { get; private set; }

    // Store original camera lock states
    private bool _originalXLock;
    private bool _originalYLock;

    // Sway/lean state
    private float _smoothedCurvature = 0f;
    private float _smoothedSpeed = 0f;
    private Vector3 _previousPathPosition;

    // Explicit yaw driven by cutscene so model methods don't need to read-back world rotation
    private float _currentModelYaw = 0f;

    // Optional delegate: given normalised t, returns the player offset vector (rope pos = player pos - offset)
    private System.Func<float, Vector3> _offsetSampler = null;
    private float _currentT = 0f;

    // Start/end orientation
    private Quaternion _pathStartRotation;
    private Quaternion _pathEndRotation;

    [Header("Orientation Settings")]
    [Tooltip("How much of the swing duration (0-1) is used to blend into upright at the end")]
    [SerializeField] private float uprightBlendFraction = 0.2f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Auto-find references
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            if (input == null) input = playerObj.GetComponent<PlayerActions>();
            if (playerRb == null) playerRb = playerObj.GetComponent<Rigidbody>();
            if (playerController == null) playerController = playerObj.GetComponent<PlayerMovement>();
            if (playerModelRotation == null) playerModelRotation = playerObj.GetComponent<PlayerModelRotationHandler>();
        }

        if (cameraController == null)
            cameraController = Camera.main?.GetComponent<ZeldaCameraController>();

        if (cameraModeController == null)
            cameraModeController = Camera.main?.GetComponent<CameraModeController>();
    }

    // Register an offset sampler so rope visuals can recover the rope attachment position.
    // Call before StartRopeSwingWithSpline. Pass null to clear.
    public void SetOffsetSampler(System.Func<float, Vector3> sampler) => _offsetSampler = sampler;

    public void StartRopeSwing(List<Transform> playerPath, float duration)
    {
        if (_isActive || playerPath == null || playerPath.Count < 2)
            return;

        _useSplinePath = false;
        _playerPath = playerPath;
        _splinePath = null;
        StartCoroutine(RopeSwingSequence(duration));
    }

    public void StartRopeSwingWithSpline(List<Vector3> splinePath, float duration)
    {
        if (_isActive || splinePath == null || splinePath.Count < 2)
            return;

        _useSplinePath = true;
        _splinePath = splinePath;
        _playerPath = null;
        StartCoroutine(RopeSwingSequence(duration));
    }

    private IEnumerator RopeSwingSequence(float duration)
    {
        _isActive = true;
        _duration = duration;
        _smoothedCurvature = 0f;
        _smoothedSpeed = 0f;
        _previousPathPosition = GetPathPosition(0f);
        CurrentPathPosition = _previousPathPosition;
        _currentT = 0f;
        _currentModelYaw = _pathStartRotation.eulerAngles.y;

        // Cache the forward rotation of the first and last path points
        _pathStartRotation = GetPathRotationFromForward(0f);
        _pathEndRotation = GetPathRotationFromForward(1f);

        // Disable player movement control
        DisablePlayerControl();

        // Stop PlayerModelRotationHandler.Update() from overwriting rotations
        if (playerModelRotation != null) playerModelRotation.SetNewRotationDir(null, true);

        // Lock camera input during swing if enabled
        if (lockCameraInput && cameraController != null)
        {
            _originalXLock = false; // We don't have getters, so assume false
            _originalYLock = false;
            cameraController.SetXAxisLocked(true);
            cameraController.SetYAxisLocked(true);
        }

        _isBlendingIn = true;
        _blendInDelayTimer = 0f;
        _blendPlayerFrom = playerRb.position;
        _blendPlayerTo = GetPathPosition(0f);
        BlendDelayActive = true;
        yield return new WaitForSeconds(blendInDelay);

        // Setup blend in - smoothly move player to start of path
        _blendStartTime = Time.time;
        BlendDelayActive = false;

        // Wait for blend in
        yield return new WaitForSeconds(blendInTime);
        _isBlendingIn = false;
        cameraController.EnterCutsceneMode();

        // Start main rope swing
        _isPlaying = true;
        _startTime = Time.time;

        // Wait for swing to complete
        yield return new WaitForSeconds(duration);
        _isPlaying = false;

        // Ensure model is left upright
        if (playerModelRotation != null) { playerModelRotation.SetSwayAngle(0f, 0f); playerModelRotation.SetLeanAngle(0f); }

        // Restore camera lock states
        if (lockCameraInput && cameraController != null)
        {
            cameraController.SetXAxisLocked(_originalXLock);
            cameraController.SetYAxisLocked(_originalYLock);
        }

        // Restore normal model rotation
        _offsetSampler = null;
        if (playerModelRotation != null) playerModelRotation.SetNewRotationDir(null, false);

        // Re-enable player control
        EnablePlayerControl();

        _isActive = false;
    }

    private void FixedUpdate()
    {
        if (!_isActive) return;

        if (_isBlendingIn)
        {
            if (_blendInDelayTimer < blendInDelay)
            {
                _blendInDelayTimer += Time.fixedDeltaTime;
                return;
            }

            // Smoothly blend player from current position to path start
            float elapsed = Time.time - _blendStartTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / blendInTime));

            Vector3 playerPos = Vector3.Lerp(_blendPlayerFrom, _blendPlayerTo, t);
            playerRb.MovePosition(playerPos);

            // Slerp rigidbody toward path start
            playerRb.MoveRotation(Quaternion.Slerp(playerRb.rotation, _pathStartRotation, t));
            // Drive model mesh directly with explicit yaw (no readback)
            if (playerModelRotation != null)
                playerModelRotation.SetSwayAngle(_pathStartRotation.eulerAngles.y, 0f);
        }
        else if (_isPlaying)
        {
            // Move player along the path
            float elapsed = Time.time - _startTime;
            float t = Mathf.Clamp01(elapsed / _duration);

            Vector3 playerPos = GetPathPosition(t);
            playerRb.MovePosition(playerPos);
            CurrentPathPosition = playerPos;
            _currentT = t;

            // Compute real world-space speed (units/sec) from frame-to-frame displacement
            float frameSpeed = Vector3.Distance(playerPos, _previousPathPosition) / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
            _previousPathPosition = playerPos;

            // Calculate direction along path and rotate to face it
            Vector3 direction = GetPathDirection(t);
            Vector3 horizontalDir = new Vector3(direction.x, 0f, direction.z).normalized;

            if (horizontalDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(horizontalDir);

                // Compute sway angle from curvature and apply to model separately
                if (enablePlayerSway && playerModelRotation != null)
                {
                    // Sample a point slightly behind to measure how much the direction has changed
                    float lookDelta = 0.05f;
                    float tBehind = Mathf.Clamp01(t - lookDelta);
                    Vector3 prevDirection = GetPathDirection(tBehind);
                    Vector3 prevHorizontal = new Vector3(prevDirection.x, 0f, prevDirection.z).normalized;

                    // Cross product Y component: positive = curving right, negative = curving left
                    float curvature = 0f;
                    if (prevHorizontal.sqrMagnitude > 0.01f)
                    {
                        Vector3 cross = Vector3.Cross(prevHorizontal, horizontalDir);
                        curvature = cross.y / lookDelta;
                    }

                    // Lerp rate derived from durationUntilMaxAngle: reaches target in ~that many seconds
                    float swayLerpRate = 1f - Mathf.Exp(-Time.fixedDeltaTime / Mathf.Max(durationUntilMaxAngle, 0.001f));
                    _smoothedCurvature = Mathf.Lerp(_smoothedCurvature, curvature, swayLerpRate);

                    // Fade sway out to zero in the final uprightBlendFraction of the swing
                    float uprightBlendStart = 1f - uprightBlendFraction;
                    float uprightFade = (t >= uprightBlendStart)
                        ? Mathf.Clamp01((t - uprightBlendStart) / uprightBlendFraction)
                        : 0f;

                    float swayAngle = Mathf.Clamp(_smoothedCurvature * maxSwayAngle, -maxSwayAngle, maxSwayAngle)
                        * (1f - uprightFade);

                    // Forward lean: driven by actual world-space speed this frame
                    // Estimate peak speed as total arc length / duration for normalisation
                    float arcLength = 0f;
                    int arcSamples = 20;
                    for (int i = 0; i < arcSamples; i++)
                        arcLength += Vector3.Distance(GetPathPosition(i / (float)arcSamples), GetPathPosition((i + 1) / (float)arcSamples));
                    float peakSpeed = arcLength / Mathf.Max(_duration, 0.001f);
                    float normalizedSpeed = Mathf.Clamp01(frameSpeed / Mathf.Max(peakSpeed, 0.001f));

                    float leanLerpRate = 1f - Mathf.Exp(-Time.fixedDeltaTime / Mathf.Max(durationUntilMaxLean, 0.001f));
                    _smoothedSpeed = Mathf.Lerp(_smoothedSpeed, normalizedSpeed, leanLerpRate);

                    float leanAngle = _smoothedSpeed * maxLeanAngle * (1f - uprightFade);

                    // Track model yaw: blend from start to end over the upright fade window
                    float endBlendStartYaw = 1f - uprightBlendFraction;
                    if (t >= endBlendStartYaw)
                    {
                        float yawBlendT = Mathf.Clamp01((t - endBlendStartYaw) / uprightBlendFraction);
                        _currentModelYaw = Mathf.LerpAngle(_currentModelYaw, _pathEndRotation.eulerAngles.y, yawBlendT);
                    }
                    else
                    {
                        _currentModelYaw = Mathf.LerpAngle(_currentModelYaw, targetRot.eulerAngles.y, 0.3f);
                    }

                    playerModelRotation.SetSwayAngle(_currentModelYaw, swayAngle);
                    playerModelRotation.SetLeanAngle(leanAngle);
                }

                // Blend rigidbody yaw toward the last point's forward in final fraction
                float endBlendStart = 1f - uprightBlendFraction;
                if (t >= endBlendStart)
                {
                    float endT = Mathf.Clamp01((t - endBlendStart) / uprightBlendFraction);
                    targetRot = Quaternion.Slerp(targetRot, _pathEndRotation, endT);
                }

                playerRb.MoveRotation(targetRot);
            }
        }
    }

    private void LateUpdate()
    {
        if (!_isActive || !autoRotateCamera || cameraController == null) return;

        if (_isBlendingIn)
        {
            // Rotate camera to face the initial movement direction
            Vector3 direction = (_blendPlayerTo - _blendPlayerFrom).normalized;
            RotateCameraToDirection(direction);
        }
        else if (_isPlaying)
        {
            // Rotate camera to face the path direction
            float elapsed = Time.time - _startTime;
            float t = Mathf.Clamp01(elapsed / _duration);

            Vector3 direction = GetPathDirection(t);
            RotateCameraToDirection(direction);
        }
    }

    private void RotateCameraToDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.01f) return;

        // Calculate target yaw from direction
        float targetYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        // Get current camera rotation
        float currentYaw = cameraController.GetCurrentYaw();
        float currentPitch = cameraController.GetCurrentPitch();

        // Smoothly interpolate to target rotation
        float newYaw = Mathf.LerpAngle(currentYaw, targetYaw, cameraRotationSpeed * Time.deltaTime);
        float newPitch = Mathf.Lerp(currentPitch, cameraPitch, cameraRotationSpeed * Time.deltaTime);

        // Apply rotation to camera
        cameraController.SetRotation(newYaw, newPitch);
    }

    private Vector3 GetPathPosition(float t)
    {
        if (_useSplinePath)
        {
            // Use pre-generated spline path
            float index = t * (_splinePath.Count - 1);
            int i = Mathf.FloorToInt(index);

            if (i >= _splinePath.Count - 1)
                return _splinePath[_splinePath.Count - 1];

            float localT = index - i;
            return Vector3.Lerp(_splinePath[i], _splinePath[i + 1], localT);
        }
        else
        {
            // Use linear interpolation between transforms
            float totalSegments = _playerPath.Count - 1;
            float scaledT = t * totalSegments;
            int currentIndex = Mathf.FloorToInt(scaledT);

            if (currentIndex >= _playerPath.Count - 1)
                return _playerPath[_playerPath.Count - 1].position;

            float segmentT = scaledT - currentIndex;
            return Vector3.Lerp(_playerPath[currentIndex].position, _playerPath[currentIndex + 1].position, segmentT);
        }
    }

    // Returns the flat (upright) rotation the player should face at the given path position.
    // For transform paths, uses the Transform's own forward. For spline paths, derives from direction.
    private Quaternion GetPathRotationFromForward(float t)
    {
        if (!_useSplinePath && _playerPath != null)
        {
            // Use the actual Transform forward of the first or last point
            int index = t <= 0f ? 0 : _playerPath.Count - 1;
            Vector3 forward = _playerPath[index].forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.001f)
                return Quaternion.LookRotation(forward.normalized);
        }

        // Fallback: derive from path direction
        Vector3 dir = GetPathDirection(t);
        Vector3 horizontal = new Vector3(dir.x, 0f, dir.z).normalized;
        if (horizontal.sqrMagnitude > 0.001f)
            return Quaternion.LookRotation(horizontal);

        return Quaternion.identity;
    }

    private Vector3 GetPathDirection(float t)
    {
        // Sample a point slightly ahead to get direction
        float lookAhead = 0.05f;
        float t2 = Mathf.Clamp01(t + lookAhead);

        Vector3 currentPos = GetPathPosition(t);
        Vector3 futurePos = GetPathPosition(t2);

        Vector3 direction = (futurePos - currentPos).normalized;

        // If we're at the very end, use the direction from the previous segment
        if (direction.sqrMagnitude < 0.01f)
        {
            if (_useSplinePath && _splinePath.Count >= 2)
            {
                direction = (_splinePath[_splinePath.Count - 1] - _splinePath[_splinePath.Count - 2]).normalized;
            }
            else if (_playerPath != null && _playerPath.Count >= 2)
            {
                direction = (_playerPath[_playerPath.Count - 1].position - _playerPath[_playerPath.Count - 2].position).normalized;
            }
        }

        return direction;
    }

    private void DisablePlayerControl()
    {
        if (input != null) input.DisableAllInput();
        if (playerController != null) playerController.enabled = false;
        if (playerRb != null) playerRb.isKinematic = true;
    }

    private void EnablePlayerControl()
    {
        if (input != null) input.EnableAllInput();
        if (playerController != null) playerController.enabled = true;
        if (playerRb != null) playerRb.isKinematic = false;
        cameraController.ExitCutsceneMode();
    }

    public bool IsActive() => _isActive;

    // Current world-space position of the player on the path (for rope visuals)
    public Vector3 CurrentPathPosition { get; private set; }

    // World-space rope attachment point: player position minus the player offset at current t
    public Vector3 CurrentRopeAttachmentPosition
    {
        get
        {
            Vector3 offset = _offsetSampler != null ? _offsetSampler(_currentT) : Vector3.zero;
            return CurrentPathPosition - offset;
        }
    }

    // Provides read access to the active spline path for visual components (e.g. LassoVisuals)
    public IReadOnlyList<Vector3> SplinePath => (_isActive && _useSplinePath) ? _splinePath : null;
}