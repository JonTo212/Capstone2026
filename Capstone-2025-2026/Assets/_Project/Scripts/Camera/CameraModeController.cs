using System.Collections.Generic;
using UnityEngine;

public enum CamState
{
    TetherEquipped,
    LassoEquipped,
    Tether,
    Lasso,
    RopeHangCutscene
}

[System.Serializable]
public struct CameraStateSettings
{
    public CamState cameraState;
    public Vector2 screenOffset; // -1 to 1
    public Vector3 targetOffset; // World-space
    public bool lockYAxis;
    public bool lockXAxis;

    public CameraStateSettings(CamState state, Vector2 screenPos, Vector3 targetOff, bool lockY, bool lockX)
    {
        cameraState = state;
        screenOffset = screenPos;
        targetOffset = targetOff;
        lockYAxis = lockY;
        lockXAxis = lockX;
    }
}

public class CameraModeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ZeldaCameraController cameraController;
    [SerializeField] private LassoTetherController lassoTetherController;
    [SerializeField] private Transform playerRef;
    [SerializeField] private CameraCutsceneHandler cameraCutsceneHandler;

    [Header("Camera State Settings")]
    [SerializeField] private List<CameraStateSettings> cameraStates = new();

    [Header("Transition Speeds")]
    [SerializeField] private float tetherModeAdjustSpeed = 5f;
    [SerializeField] private float lassoModeAdjustSpeed = 8f;
    [SerializeField] private float defaultAdjustSpeed = 6f;

    [Header("Sensitivity Modifiers")]
    [SerializeField] private float tetherSensMultiplier = 0.9f;
    [SerializeField] private float lassoSensMultiplier = 0.75f;

    [Header("Lasso Mode Dynamic Zoom")]
    [SerializeField] private float playerDefaultYOffset = 0.25f;
    [SerializeField] private float zoomPadding = 0.5f;
    [SerializeField] private float lassoZoomOutSmoothTime = 0.1f;

    [Header("Lasso Mode Pitch Clamp")]
    [SerializeField] private float lassoMinPitch = -30f;
    [SerializeField] private float lassoMaxPitch = 30f;

    [Header("Lasso Mode Framing")]
    [SerializeField] private float lassoMaxZoomOffset = 5f;
    [SerializeField] private float lassoTargetOffsetY = 1.5f;
    [SerializeField] private float lassoScreenOffsetY = -0.2f;

    private Vector2 defaultScreenOffset;
    private Vector3 defaultTargetOffset;
    private float defaultDistance;
    private float baseSensitivityX;
    private float baseSensitivityY;

    private Vector2 currentScreenOffset;
    private Vector3 currentTargetOffset;
    private float currentTargetDistance;
    private bool hasSnappedToLasso;
    private bool _lassoAboveClamp;
    private bool _lassoBelowClamp;
    private float _currentDistanceVelocity;

    private void Awake()
    {
        if (cameraController == null)
            cameraController = GetComponent<ZeldaCameraController>();

        defaultScreenOffset = cameraController.GetScreenOffset();
        defaultTargetOffset = cameraController.GetTargetOffset();
        defaultDistance = cameraController.GetDefaultDistance();

        currentScreenOffset = defaultScreenOffset;
        currentTargetOffset = defaultTargetOffset;
        currentTargetDistance = defaultDistance;

        baseSensitivityX = cameraController.GetMouseXSensitivity();
        baseSensitivityY = cameraController.GetMouseYSensitivity();
    }

    private void Update()
    {
        if (cameraCutsceneHandler != null && cameraCutsceneHandler.IsActive())
            return;

        switch (lassoTetherController.CurrentLassoState)
        {
            case LassoState.Snared:
            case LassoState.FreeRotating:
                LassoModeCamera();
                break;

            case LassoState.Empty:
                ResetCamera();
                break;

            case LassoState.Tethering:
            case LassoState.SnaredTether:
                TetherModeCamera();
                break;
        }
    }


    private void LassoModeCamera()
    {
        Prop snaredProp = lassoTetherController.Lasso.SnaredObject;
        if (snaredProp == null) { ApplySensitivity(lassoSensMultiplier, lassoSensMultiplier); return; }

        // Camera system owns vertical object positioning in lasso mode
        lassoTetherController.Lasso.SuppressLiftInput = true;

        cameraController.SetDistanceLimit(25f);
        cameraController.SetCollisionSmoothTimeOverride(lassoZoomOutSmoothTime);

        if (!hasSnappedToLasso)
        {
            cameraController.SetPitchSmoothOverride(lassoZoomOutSmoothTime);
            float clampedPitch = Mathf.Clamp(cameraController.GetCurrentPitch(), lassoMinPitch, lassoMaxPitch);
            cameraController.SetRotation(cameraController.GetCurrentYaw(), clampedPitch);
            hasSnappedToLasso = true;
        }

        // Determine y-axis lock BEFORE calling ApplyCameraSettings, which also sets it.
        // We handle it ourselves here so we can override after.
        bool aboveClampYLock = HandleAboveClampInput();

        bool rotateMode = lassoTetherController.CurrentLassoState == LassoState.FreeRotating;

        float requiredDistanceOffset = CalculateRequiredDistanceOffset(
            playerRef.position, snaredProp.transform.position);
        currentTargetDistance = Mathf.SmoothDamp(
            currentTargetDistance,
            defaultDistance + requiredDistanceOffset,
            ref _currentDistanceVelocity,
            lassoZoomOutSmoothTime);
        cameraController.SetCurrentDistance(currentTargetDistance);
        cameraController.SetDistanceLimit(currentTargetDistance);

        Vector3 lassoTargetOffset = defaultTargetOffset + new Vector3(0f, lassoTargetOffsetY, 0f);
        Vector2 lassoScreenOffset = new Vector2(0f, lassoScreenOffsetY);
        ApplyCameraSettings(lassoScreenOffset, lassoTargetOffset, false, rotateMode, lassoModeAdjustSpeed);

        // Override the Y axis lock set by ApplyCameraSettings
        cameraController.SetYAxisLocked(aboveClampYLock);

        // Apply pitch clamp after input has been evaluated
        cameraController.SetVerticalClamp(lassoMinPitch, lassoMaxPitch);

        ApplySensitivity(lassoSensMultiplier, lassoSensMultiplier);
    }

    /// <summary>
    /// When pitch is at lassoMaxPitch and the player pitches up further, the extra input lifts
    /// the object above the crosshair via liftOffset instead of moving the camera.
    /// When liftOffset > 0 and the player pitches down, that input lowers the object first;
    /// once liftOffset reaches 0, normal camera pitch resumes.
    /// Returns true if the Y axis should be locked (camera should not pitch).
    /// </summary>
    private bool HandleAboveClampInput()
    {
        Lasso lasso = lassoTetherController.Lasso;
        if (lasso == null) return false;

        // HandleInput does: targetPitch -= mouseY
        // Pitch positive = looking DOWN. mouseY > 0 = pitch decreases = looking UP.
        // GetRawLookInputY returns input.LookInput.y, same sign as mouseY.
        // rawLookY > 0 = looking UP, rawLookY < 0 = looking DOWN.
        // lassoMinPitch = upward limit (most negative). lassoMaxPitch = downward limit (most positive).
        float rawLookY = cameraController.GetRawLookInputY();
        bool lookingUp = rawLookY > 0.001f;
        bool lookingDown = rawLookY < -0.001f;

        bool atMinPitch = cameraController.GetTargetPitch() <= lassoMinPitch + 0.01f;
        bool atMaxPitch = cameraController.GetTargetPitch() >= lassoMaxPitch - 0.01f;

        if (_lassoAboveClamp)
        {
            if (lookingDown)
            {
                // rawLookY negative when looking down; negate for positive magnitude, subtract from lift
                float delta = -rawLookY * baseSensitivityY * lasso.LiftSpeed * Time.deltaTime;
                lasso.AddLiftOffset(-delta);
                lasso.SetVerticalAnchor(false);

                if (lasso.CurrentLiftOffset <= 0f)
                {
                    lasso.AddLiftOffset(-lasso.CurrentLiftOffset);
                    _lassoAboveClamp = false;
                }
                return true;
            }
            else
            {
                if (lookingUp)
                {
                    float delta = rawLookY * baseSensitivityY * lasso.LiftSpeed * Time.deltaTime;
                    lasso.AddLiftOffset(delta);
                    lasso.SetVerticalAnchor(false);
                }
                return true;
            }
        }
        else if (_lassoBelowClamp)
        {
            if (lookingUp)
            {
                // rawLookY positive when looking up; liftOffset is negative here, bring it back to 0
                float delta = rawLookY * baseSensitivityY * lasso.LiftSpeed * Time.deltaTime;
                lasso.AddLiftOffset(delta);
                lasso.SetVerticalAnchor(false);

                if (lasso.CurrentLiftOffset >= 0f)
                {
                    lasso.AddLiftOffset(-lasso.CurrentLiftOffset);
                    _lassoBelowClamp = false;
                }
                return true;
            }
            else
            {
                if (lookingDown)
                {
                    float delta = -rawLookY * baseSensitivityY * lasso.LiftSpeed * Time.deltaTime;
                    lasso.AddLiftOffset(-delta);
                    lasso.SetVerticalAnchor(false);
                }
                return true;
            }
        }
        else
        {
            if (atMinPitch && lookingUp)
            {
                _lassoAboveClamp = true;
                float delta = rawLookY * baseSensitivityY * lasso.LiftSpeed * Time.deltaTime;
                lasso.AddLiftOffset(delta);
                lasso.SetVerticalAnchor(false);
                return true;
            }

            if (atMaxPitch && lookingDown)
            {
                _lassoBelowClamp = true;
                float delta = -rawLookY * baseSensitivityY * lasso.LiftSpeed * Time.deltaTime;
                lasso.AddLiftOffset(-delta);
                lasso.SetVerticalAnchor(false);
                return true;
            }

            if (lasso.CurrentLiftOffset > 0f)
                lasso.AddLiftOffset(-lasso.CurrentLiftOffset);

            return false;
        }
    }

    private void TetherModeCamera()
    {
        cameraController.SetDistanceLimit(10f);
        ApplyCameraStateSettings(CamState.Tether, tetherModeAdjustSpeed);
        ApplySensitivity(tetherSensMultiplier, tetherSensMultiplier);
    }

    private void ResetCamera()
    {
        cameraController.SetCollisionSmoothTimeOverride(null);
        cameraController.SetPitchSmoothOverride(null);
        cameraController.SetDistanceLimit(defaultDistance);
        cameraController.SetVerticalClamp(null, null);
        cameraController.SetYAxisLocked(false);
        if (lassoTetherController.Lasso != null)
            lassoTetherController.Lasso.SuppressLiftInput = false;
        hasSnappedToLasso = false;
        _lassoAboveClamp = false;
        _lassoBelowClamp = false;

        ApplyCameraStateSettings(
            lassoTetherController.rodEquipped ? CamState.LassoEquipped : CamState.TetherEquipped,
            defaultAdjustSpeed);

        currentTargetDistance = defaultDistance;
        ApplySensitivity(1f, 1f);
    }


    private void ApplyCameraStateSettings(CamState state, float speed)
    {
        CameraStateSettings? s = GetSettingsForState(state);
        if (s.HasValue)
            ApplyCameraSettings(s.Value.screenOffset, defaultTargetOffset + s.Value.targetOffset,
                                s.Value.lockYAxis, s.Value.lockXAxis, speed);
        else
            ApplyCameraSettings(defaultScreenOffset, defaultTargetOffset, false, false, speed);
    }

    private void ApplyCameraSettings(Vector2 targetScreen, Vector3 targetOffset,
                                     bool lockY, bool lockX, float speed)
    {
        currentScreenOffset = Vector2.Lerp(currentScreenOffset, targetScreen, speed * Time.deltaTime);
        currentTargetOffset = Vector3.Lerp(currentTargetOffset, targetOffset, speed * Time.deltaTime);

        cameraController.SetScreenOffset(currentScreenOffset);
        cameraController.SetTargetOffset(currentTargetOffset);
        cameraController.SetYAxisLocked(lockY);
        cameraController.SetXAxisLocked(lockX);
    }

    private float CalculateRequiredDistanceOffset(Vector3 playerPos, Vector3 objectPos)
    {
        // Raw vertical separation between player and object
        float verticalSep = objectPos.y - playerPos.y;

        // playerDefaultYOffset (0..1) is where the player sits in the frame.
        // If the object is above the player (verticalSep > 0), it occupies the upper
        // (1 - playerDefaultYOffset) of the frame. Scale separation accordingly so the
        // zoom accounts for how much frame space the object actually has.
        float frameFraction = verticalSep >= 0f
            ? (1f - playerDefaultYOffset)
            : playerDefaultYOffset;
        float biasedSep = frameFraction > 0.001f ? Mathf.Abs(verticalSep) / frameFraction : Mathf.Abs(verticalSep);

        float separation = biasedSep * zoomPadding;
        float requiredDistance = Mathf.Max(separation, defaultDistance);
        return Mathf.Min(requiredDistance - defaultDistance, lassoMaxZoomOffset);
    }

    private CameraStateSettings? GetSettingsForState(CamState state)
    {
        foreach (var s in cameraStates)
            if (s.cameraState == state) return s;
        return null;
    }

    private void ApplySensitivity(float xMult, float yMult)
    {
        cameraController.SetMouseSensitivity(baseSensitivityX * xMult, baseSensitivityY * yMult);
    }
}