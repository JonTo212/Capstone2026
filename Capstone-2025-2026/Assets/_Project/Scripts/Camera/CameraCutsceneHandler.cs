using System.Collections;
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


    private bool _isActive = false;
    private bool _isPlaying = false;

    public bool IsActive() => _isActive;
    public bool IsPlaying() => _isPlaying;

    private CutsceneBase _cutscene;
    private float _duration;
    private float _startTime;

    private bool _isBlendingIn = false;
    private float _blendStartTime;
    private Vector3 _blendPlayerFrom;
    private Vector3 _blendPlayerTo;
    public bool BlendDelayActive { get; private set; }
    public bool BlendingIn => _isBlendingIn;
    public CutsceneBase CurrentCutscene => _cutscene;

    private Vector3 _previousPathPosition;
    private float _currentT = 0f;

    public Vector3 CurrentPathPosition { get; private set; }

    public Vector3 CurrentRopeAttachmentPosition
    {
        get
        {
            if (_cutscene is RopeSwingCutscene rope)
                return CurrentPathPosition - rope.SampleOffsetAtT(_currentT);
            return CurrentPathPosition;
        }
    }


    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            if (input == null) input = playerObj.GetComponent<PlayerActions>();
            if (playerRb == null) playerRb = playerObj.GetComponent<Rigidbody>();
            if (playerController == null) playerController = playerObj.GetComponent<PlayerMovement>();
            if (playerModelRotation == null) playerModelRotation = playerObj.GetComponent<PlayerModelRotationHandler>();
        }

        if (cameraController == null) cameraController = Camera.main?.GetComponent<ZeldaCameraController>();
        if (cameraModeController == null) cameraModeController = Camera.main?.GetComponent<CameraModeController>();
    }

    public void StartCutscene(CutsceneBase cutscene)
    {
        if (_isActive) return;
        if (cutscene == null) return;
        if (!cutscene.IsValid()) return;

        _cutscene = cutscene;
        StartCoroutine(CutsceneSequence());
    }


    private IEnumerator CutsceneSequence()
    {
        _isActive = true;
        _duration = _cutscene.duration;
        _currentT = 0f;

        _cutscene.OnCutscenePrepare();

        if (_cutscene.disablePlayerControl)
            DisablePlayerControl();

        if (playerModelRotation != null)
            playerModelRotation.SetNewRotationDir(null, true);

        if (_cutscene.lockCameraInput && cameraController != null)
        {
            cameraController.SetXAxisLocked(true);
            cameraController.SetYAxisLocked(true);
        }

        _isBlendingIn = true;
        BlendDelayActive = true;
        _blendPlayerFrom = playerRb != null ? playerRb.position : Vector3.zero;
        _blendPlayerTo = _cutscene.GetPlayerPosition(0f);
        _previousPathPosition = _blendPlayerTo;
        CurrentPathPosition = _blendPlayerTo;

        yield return new WaitForSeconds(_cutscene.blendInDelay);
        BlendDelayActive = false;

        _blendStartTime = Time.time;

        yield return new WaitForSeconds(_cutscene.blendInTime);
        _isBlendingIn = false;

        cameraController?.EnterCutsceneMode();
        _cutscene.OnCutsceneStart();

        _isPlaying = true;
        _startTime = Time.time;

        yield return new WaitForSeconds(_duration);

        _isPlaying = false;

        _cutscene.OnCutsceneEnd();

        if (_cutscene.lockCameraInput && cameraController != null)
        {
            cameraController.SetXAxisLocked(false);
            cameraController.SetYAxisLocked(false);
        }

        if (playerModelRotation != null)
        {
            playerModelRotation.SetSwayAngle(0f, 0f);
            playerModelRotation.SetLeanAngle(0f);
            playerModelRotation.SetNewRotationDir(null, false);
        }

        if (_cutscene.disablePlayerControl)
            EnablePlayerControl();

        _cutscene = null;
        _isActive = false;
    }


    private void FixedUpdate()
    {
        if (!_isActive || _cutscene == null) return;

        if (_isBlendingIn)
        {
            if (!BlendDelayActive)
            {
                float elapsed = Time.time - _blendStartTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / _cutscene.blendInTime));

                if (_cutscene.disablePlayerControl && playerRb != null)
                {
                    playerRb.MovePosition(Vector3.Lerp(_blendPlayerFrom, _blendPlayerTo, t));

                    if (_cutscene is RopeSwingCutscene rope)
                        playerRb.MoveRotation(Quaternion.Slerp(playerRb.rotation, rope.PathStartRotation, t));
                }
            }
            return;
        }

        if (!_isPlaying) return;

        float elapsed2 = Time.time - _startTime;
        _currentT = Mathf.Clamp01(elapsed2 / _duration);

        Vector3 pos = _cutscene.GetPlayerPosition(_currentT);
        float speed = Vector3.Distance(pos, _previousPathPosition)
                        / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
        _previousPathPosition = pos;
        CurrentPathPosition = pos;

        if (_cutscene.disablePlayerControl && playerRb != null)
        {
            playerRb.MovePosition(pos);

            if (_cutscene is RopeSwingCutscene rope)
                playerRb.MoveRotation(rope.GetPlayerBodyRotation(_currentT));
        }

        if (_cutscene is RopeSwingCutscene ropeAnim)
            ropeAnim.TickAnimation(_currentT, speed, playerModelRotation);
        else
            _cutscene.OnCutsceneTick(_currentT, pos, speed);
    }


    private void LateUpdate()
    {
        if (!_isActive || _cutscene == null) return;

        _cutscene.OnCutsceneLateUpdate();

        if (!_cutscene.autoRotateCamera || cameraController == null) return;

        Vector3 direction = Vector3.zero;

        if (_isBlendingIn)
        {
            direction = (_blendPlayerTo - _blendPlayerFrom).normalized;
        }
        else if (_isPlaying)
        {
            if (_cutscene is RopeSwingCutscene rope)
                direction = rope.GetPathDirection(_currentT);
        }

        if (direction.sqrMagnitude > 0.01f)
            RotateCameraToDirection(direction, _cutscene.cameraRotationSpeed, _cutscene.cameraPitch);
    }


    private void RotateCameraToDirection(Vector3 direction, float speed, float pitch)
    {
        float targetYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        float currentYaw = cameraController.GetCurrentYaw();
        float currentPitch = cameraController.GetCurrentPitch();

        float newYaw = Mathf.LerpAngle(currentYaw, targetYaw, speed * Time.deltaTime);
        float newPitch = Mathf.Lerp(currentPitch, pitch, speed * Time.deltaTime);

        cameraController.SetRotation(newYaw, newPitch);
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
        cameraController?.ExitCutsceneMode();
    }
}