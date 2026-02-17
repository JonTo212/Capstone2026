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
    [SerializeField] private ZeldaCameraController cameraController;
    [SerializeField] private CameraModeController cameraModeController;

    [Header("Transition Settings")]
    [SerializeField] private float blendInTime = 0.5f;

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

    [Tooltip("Speed of the sway oscillation")]
    [SerializeField] private float swaySpeed = 2f;

    private bool _isActive = false;
    private bool _isPlaying = false;

    // Path data
    private float _startTime;
    private float _duration;
    private List<Transform> _playerPath;
    private List<Vector3> _splinePath;
    private bool _useSplinePath = false;

    // Blend state
    private bool _isBlendingIn = false;
    private float _blendStartTime;
    private Vector3 _blendPlayerFrom;
    private Vector3 _blendPlayerTo;

    // Store original camera lock states
    private bool _originalXLock;
    private bool _originalYLock;

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
        }

        if (cameraController == null)
            cameraController = Camera.main?.GetComponent<ZeldaCameraController>();

        if (cameraModeController == null)
            cameraModeController = Camera.main?.GetComponent<CameraModeController>();
    }

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

        // Disable player movement control
        DisablePlayerControl();

        // Lock camera input during swing if enabled
        if (lockCameraInput && cameraController != null)
        {
            _originalXLock = false; // We don't have getters, so assume false
            _originalYLock = false;
            cameraController.SetXAxisLocked(true);
            cameraController.SetYAxisLocked(true);
        }

        // Setup blend in - smoothly move player to start of path
        _blendPlayerFrom = playerRb.position;
        _blendPlayerTo = GetPathPosition(0f);

        _isBlendingIn = true;
        _blendStartTime = Time.time;

        // Wait for blend in
        yield return new WaitForSeconds(blendInTime);
        _isBlendingIn = false;

        // Start main rope swing
        _isPlaying = true;
        _startTime = Time.time;

        // Wait for swing to complete
        yield return new WaitForSeconds(duration);
        _isPlaying = false;

        // Restore camera lock states
        if (lockCameraInput && cameraController != null)
        {
            cameraController.SetXAxisLocked(_originalXLock);
            cameraController.SetYAxisLocked(_originalYLock);
        }

        // Re-enable player control
        EnablePlayerControl();

        _isActive = false;
    }

    private void FixedUpdate()
    {
        if (!_isActive) return;

        if (_isBlendingIn)
        {
            // Smoothly blend player from current position to path start
            float elapsed = Time.time - _blendStartTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / blendInTime));

            Vector3 playerPos = Vector3.Lerp(_blendPlayerFrom, _blendPlayerTo, t);
            playerRb.MovePosition(playerPos);

            // Rotate to face direction of movement
            Vector3 direction = (_blendPlayerTo - _blendPlayerFrom).normalized;
            direction.y = 0;
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                playerRb.MoveRotation(Quaternion.Slerp(playerRb.rotation, targetRot, t));
            }
        }
        else if (_isPlaying)
        {
            // Move player along the path
            float elapsed = Time.time - _startTime;
            float t = Mathf.Clamp01(elapsed / _duration);

            Vector3 playerPos = GetPathPosition(t);
            playerRb.MovePosition(playerPos);

            // Calculate direction along path and rotate to face it
            Vector3 direction = GetPathDirection(t);
            Vector3 horizontalDir = new Vector3(direction.x, 0f, direction.z).normalized;

            if (horizontalDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(horizontalDir);

                // Add sway if enabled
                if (enablePlayerSway)
                {
                    float swayAngle = Mathf.Sin(Time.time * swaySpeed) * maxSwayAngle;
                    targetRot *= Quaternion.Euler(0f, 0f, swayAngle);
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
    }

    public bool IsActive() => _isActive;
}