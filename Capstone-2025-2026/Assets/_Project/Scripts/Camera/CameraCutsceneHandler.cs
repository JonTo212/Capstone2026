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
    [SerializeField] private LassoVisuals lassoVisuals;

    [Header("Rope Swing Sway Offset")]
    [Tooltip("How far (in world units) the player is nudged sideways per degree of camera sway tilt.")]
    [SerializeField] private float swayPositionStrength = 0.02f;


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
    private bool _hasSnappedBlendRotation = false;
    public bool BlendDelayActive { get; private set; }
    public bool BlendingIn => _isBlendingIn;
    public CutsceneBase CurrentCutscene => _cutscene;
    private RopeSwingCutscene RopeCutscene => _cutscene as RopeSwingCutscene;

    private Vector3 _previousPathPosition;
    private float _currentT = 0f;

    public Vector3 CurrentPathPosition { get; private set; }

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

        if (RopeCutscene != null) cameraController.SetPositionDamping(Vector3.zero);

        yield return new WaitForSeconds(_cutscene.blendInDelay);
        BlendDelayActive = false;

        _blendStartTime = Time.time;

        yield return new WaitForSeconds(_cutscene.blendInTime);
        _isBlendingIn = false;

        _cutscene.OnCutsceneStart();

        _isPlaying = true;
        _startTime = Time.time;

        yield return new WaitForSeconds(_duration);

        if (RopeCutscene != null) cameraController.SetPositionDamping(null);
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

        // Tell lasso visuals to resume normal drawing
        if (lassoVisuals != null)
            lassoVisuals.ExitCutsceneMode();

        _hasSnappedBlendRotation = false;
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

                    if (RopeCutscene != null)
                    {
                        // Snap the player to face the path start on the very first frame after
                        // the blend delay clears, then hold that rotation for the rest of blend-in.
                        if (!_hasSnappedBlendRotation)
                        {
                            playerRb.MoveRotation(RopeCutscene.PathStartRotation);
                            _hasSnappedBlendRotation = true;
                        }
                    }
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
            // Nudge the player sideways to match the camera's sway tilt.
            // TickAnimation hasn't run yet this frame, so we use last frame's sway angle.
            Vector3 swayPos = pos;
            if (RopeCutscene != null && swayPositionStrength > 0f)
            {
                Vector3 travelDir = RopeCutscene.GetPathDirection(_currentT);
                Vector3 right = Vector3.Cross(Vector3.up, travelDir).normalized;
                swayPos -= right * (RopeCutscene.CurrentSwayAngle * swayPositionStrength);
            }

            playerRb.MovePosition(swayPos);

            if (RopeCutscene != null)
                playerRb.MoveRotation(RopeCutscene.GetPlayerBodyRotation(_currentT));
        }

        if (RopeCutscene != null)
            RopeCutscene.TickAnimation(_currentT, speed, playerModelRotation);
        else
            _cutscene.OnCutsceneTick(_currentT, pos, speed);
    }


    private void LateUpdate()
    {
        if (!_isActive || _cutscene == null) return;

        // Update rope attachment position and drive lasso visuals from here,
        // after FixedUpdate has already moved the rigidbody via MovePosition().
        // This eliminates the one-frame stutter from LassoVisuals polling independently.
        if (RopeCutscene != null && !BlendDelayActive)
        {
            Vector3 renderPos = GetRenderPathPosition();
            RopeCutscene.UpdateRopeVisuals(renderPos, _currentT);

            if (lassoVisuals != null)
                lassoVisuals.DrawCutsceneRopeExternal(
                    lassoVisuals.GetHoldPos(),
                    RopeCutscene.CurrentRopeAttachmentPosition);
        }

        if (!_cutscene.autoRotateCamera || cameraController == null) return;

        // Do not rotate the camera during blend-in - the player may be approaching
        // from any angle, so rotating here would snap the camera sideways.
        // Only begin auto-rotation once the cutscene is actively playing.
        if (!_isPlaying) return;

        Vector3 direction = RopeCutscene != null
            ? RopeCutscene.GetPathDirection(_currentT)
            : Vector3.zero;

        if (direction.sqrMagnitude > 0.01f)
            RotateCameraToDirection(direction, _cutscene.cameraRotationSpeed, _cutscene.cameraPitch);

        _cutscene.OnCutsceneLateUpdate();
    }

    public Vector3 GetRenderPathPosition()
    {
        if (RopeCutscene == null || !_isPlaying) return CurrentPathPosition;
        float elapsed = Time.time - _startTime;
        float t = Mathf.Clamp01(elapsed / _duration);
        return _cutscene.GetPlayerPosition(t);
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
        if (playerRb != null)
        {
            playerRb.isKinematic = false;
            playerRb.linearVelocity = Vector3.zero;
        }
    }
}