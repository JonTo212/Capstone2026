using System.Collections.Generic;
using UnityEngine;

public enum CamState
{
    TetherEquipped,
    LassoEquipped,
    Tether,
    Lasso,
}

[System.Serializable]
public struct CameraStateSettings
{
    public CamState cameraState;
    public Vector2 screenOffset; // -1 to 1
    public Vector3 targetOffset; // World-space
    public float distanceOffset; // Zoom relative to default
    public bool lockYAxis;
    public bool lockXAxis;

    public CameraStateSettings(CamState state, Vector2 screenPos, Vector3 targetOff, float distOff, bool lockY, bool lockX)
    {
        cameraState = state;
        screenOffset = screenPos;
        targetOffset = targetOff;
        distanceOffset = distOff;
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

    [Header("Camera State Settings")]
    [SerializeField] private List<CameraStateSettings> cameraStates = new();

    [Header("Transition Speeds")]
    [SerializeField] private float tetherModeAdjustSpeed = 5f;
    [SerializeField] private float lassoModeAdjustSpeed = 8f;
    [SerializeField] private float defaultAdjustSpeed = 6f;

    [Header("Sensitivity Modifiers")]
    [SerializeField] private float tetherSensMultiplier = 0.9f;
    [SerializeField] private float lassoSensMultiplier = 0.75f;

    [Header("Lasso Mode Dynamic Zoom Settings")]
    [SerializeField] private float playerDefaultYOffset = 0.25f;
    [SerializeField] private float zoomPadding = 1.15f;
    [SerializeField] private float lassoZoomOutSmoothTime = 0.1f;

    [Header("Lasso Mode Pitch Clamp")]
    [SerializeField] private float lassoMinPitch = -30f;
    [SerializeField] private float lassoMaxPitch = 30f;


    private bool _lassoAboveClamp;
    private bool _lassoBelowClamp;
    private float _currentDistanceVelocity;

    //cached values
    private Vector2 defaultScreenOffset;
    private Vector3 defaultTargetOffset;
    private float defaultDistance;
    private float characterHeight;
    private float baseSensitivityX;
    private float baseSensitivityY;
    private bool hasSnappedToLasso;
    private bool _wasInCutscene;

    //framing, for lasso
    private Vector2 currentScreenOffset;
    private Vector3 currentTargetOffset;
    private float currentDistanceOffset;
    private float currentTargetDistance;
    private Vector2 optimalFraming;

    private void Awake()
    {
        if (cameraController == null)
            cameraController = GetComponent<ZeldaCameraController>();

        defaultScreenOffset = cameraController.GetScreenOffset();
        defaultTargetOffset = cameraController.GetTargetOffset();
        defaultDistance = cameraController.GetDefaultDistance();

        currentScreenOffset = defaultScreenOffset;
        currentTargetOffset = defaultTargetOffset;
        currentDistanceOffset = 0f;
        currentTargetDistance = defaultDistance;

        if (playerRef != null)
        {
            CapsuleCollider col = playerRef.GetComponent<CapsuleCollider>();
            if (col != null) characterHeight = col.height;
        }

        baseSensitivityX = cameraController.GetMouseXSensitivity();
        baseSensitivityY = cameraController.GetMouseYSensitivity();
    }

    private void Update()
    {
        if (CameraCutsceneHandler.Instance != null && CameraCutsceneHandler.Instance.IsActive())
        {
            CutsceneBase current = CameraCutsceneHandler.Instance.CurrentCutscene;
            if (current != null && current is RopeSwingCutscene)
            {
                _wasInCutscene = true;
                return;
            }
        }

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
        if (snaredProp != null)
        {
            cameraController.SetDistanceLimit(25f);
            lassoTetherController.Lasso.SuppressLiftInput = true;

            float requiredDistanceOffset = CalculateRequiredDistanceOffset(playerRef.position, snaredProp.transform.position, out optimalFraming);

            CameraStateSettings? lassoSettings = GetSettingsForState(CamState.Lasso);
            Vector3 lassoBaseOffset = lassoSettings.HasValue ? defaultTargetOffset + lassoSettings.Value.targetOffset : defaultTargetOffset;
            Vector2 lassoBaseScreenOffset = lassoSettings.HasValue ? lassoSettings.Value.screenOffset : defaultScreenOffset;
            float lassoBaseDistance = defaultDistance + (lassoSettings.HasValue ? lassoSettings.Value.distanceOffset : 0f);
            currentTargetDistance = lassoBaseDistance + requiredDistanceOffset;

            bool rotateMode = lassoTetherController.CurrentLassoState == LassoState.FreeRotating;
            bool yLocked = HandleAboveClampInput();
            ApplyCameraSettings(lassoBaseScreenOffset + optimalFraming, lassoBaseOffset, currentTargetDistance, false, rotateMode, lassoModeAdjustSpeed);
            cameraController.SetCollisionSmoothTimeOverride(lassoZoomOutSmoothTime);
            cameraController.SetYAxisLocked(yLocked);
            cameraController.SetVerticalClamp(lassoMinPitch, lassoMaxPitch);

            if (!hasSnappedToLasso)
            {
                cameraController.SetPitchSmoothOverride(lassoZoomOutSmoothTime);
                float clampedPitch = Mathf.Clamp(cameraController.GetCurrentPitch(), lassoMinPitch, lassoMaxPitch);
                cameraController.SetRotation(cameraController.GetCurrentYaw(), clampedPitch);
                hasSnappedToLasso = true;
            }
        }

        ApplySensitivity(lassoSensMultiplier, lassoSensMultiplier);
    }

    private void TetherModeCamera()
    {
        cameraController.SetDistanceLimit(10f);
        ApplyCameraStateSettings(CamState.Tether, tetherModeAdjustSpeed);
        ApplySensitivity(tetherSensMultiplier, tetherSensMultiplier);
    }

    private void ResetCamera()
    {
        if (_wasInCutscene)
        {
            currentScreenOffset = cameraController.GetScreenOffset();
            currentTargetOffset = cameraController.GetTargetOffset();
            _wasInCutscene = false;
        }

        cameraController.SetCollisionSmoothTimeOverride(null);
        cameraController.SetPitchSmoothOverride(null);
        cameraController.SetDistanceLimit(defaultDistance);
        hasSnappedToLasso = false;

        ApplyCameraStateSettings(
            lassoTetherController.rodEquipped ? CamState.LassoEquipped : CamState.TetherEquipped,
            defaultAdjustSpeed);

        currentTargetDistance = defaultDistance;
        ApplySensitivity(1f, 1f);

        cameraController.SetVerticalClamp(null, null);
        _lassoAboveClamp = false;
        _lassoBelowClamp = false;
        if (lassoTetherController.Lasso != null)
            lassoTetherController.Lasso.SuppressLiftInput = false;
    }

    private void ApplyCameraStateSettings(CamState state, float smoothSpeed)
    {
        CameraStateSettings? settings = GetSettingsForState(state);
        if (settings.HasValue)
        {
            float targetDistance = defaultDistance + settings.Value.distanceOffset;
            ApplyCameraSettings(settings.Value.screenOffset, defaultTargetOffset + settings.Value.targetOffset, targetDistance, settings.Value.lockYAxis, settings.Value.lockXAxis, smoothSpeed);
        }
        else
        {
            ApplyCameraSettings(defaultScreenOffset, defaultTargetOffset, defaultDistance, false, false, smoothSpeed);
        }
    }

    private void ApplyCameraSettings(Vector2 targetScreenOffset, Vector3 targetOffset, float targetDistance, bool lockY, bool lockX, float smoothSpeed)
    {
        currentScreenOffset = Vector2.Lerp(currentScreenOffset, targetScreenOffset, smoothSpeed * Time.deltaTime);
        currentTargetOffset = Vector3.Lerp(currentTargetOffset, targetOffset, smoothSpeed * Time.deltaTime);
        currentDistanceOffset = Mathf.Lerp(currentDistanceOffset, targetDistance - defaultDistance, smoothSpeed * Time.deltaTime);

        cameraController.SetScreenOffset(currentScreenOffset);
        cameraController.SetTargetOffset(currentTargetOffset);
        cameraController.SetDistance(defaultDistance + currentDistanceOffset);

        cameraController.SetYAxisLocked(lockY);
        cameraController.SetXAxisLocked(lockX);
    }

    private float CalculateRequiredDistanceOffset(Vector3 playerPos, Vector3 objectPos, out Vector2 optimalFraming)
    {
        Camera mainCam = Camera.main;
        float playerBottom = playerPos.y - (characterHeight / 2f);
        float playerTop = playerPos.y + (characterHeight / 2f);
        float objectBottom = objectPos.y;
        float objectTop = objectPos.y;

        Renderer objRenderer = lassoTetherController.Lasso.SnaredObject.GetComponent<Renderer>();
        if (objRenderer != null)
        {
            objectBottom = objRenderer.bounds.min.y;
            objectTop = objRenderer.bounds.max.y;
        }

        float lowestY = Mathf.Min(playerBottom, objectBottom);
        float highestY = Mathf.Max(playerTop, objectTop);
        float verticalSpan = (highestY - lowestY) * zoomPadding;
        float fovRad = mainCam.fieldOfView * Mathf.Deg2Rad;
        float requiredDistance = Mathf.Max(verticalSpan / (2f * Mathf.Tan(fovRad / 2f)), defaultDistance);

        Vector3 playerViewport = mainCam.WorldToViewportPoint(playerPos);
        float delta = playerViewport.y - playerDefaultYOffset;

        float normalized = Mathf.Clamp(delta * 2f, -1f, 1f);
        optimalFraming = new Vector2(0f, normalized);

        return requiredDistance - defaultDistance;
    }

    private float CalculateLiftDelta(float rawLookY, Lasso lasso)
    {
        float currentPitch = cameraController.GetCurrentPitch();
        float newPitch = currentPitch - rawLookY * baseSensitivityY;
        float currentY = Mathf.Sin(currentPitch * Mathf.Deg2Rad) * lasso.AnchorDist;
        float newY = Mathf.Sin(newPitch * Mathf.Deg2Rad) * lasso.AnchorDist;
        return currentY - newY;
    }

    private bool HandleAboveClampInput()
    {
        Lasso lasso = lassoTetherController.Lasso;
        if (lasso == null) return false;

        float rawLookY = cameraController.GetRawLookInputY();
        bool lookingUp = rawLookY > 0.001f;
        bool lookingDown = rawLookY < -0.001f;

        bool atMinPitch = cameraController.GetTargetPitch() <= lassoMinPitch + 0.01f;
        bool atMaxPitch = cameraController.GetTargetPitch() >= lassoMaxPitch - 0.01f;

        if (_lassoAboveClamp)
        {
            if (lookingDown)
            {
                lasso.AddLiftOffset(CalculateLiftDelta(rawLookY, lasso));
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
                    lasso.AddLiftOffset(CalculateLiftDelta(rawLookY, lasso));
                    lasso.SetVerticalAnchor(false);
                }
                return true;
            }
        }
        else if (_lassoBelowClamp)
        {
            if (lookingUp)
            {
                lasso.AddLiftOffset(CalculateLiftDelta(rawLookY, lasso));
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
                    lasso.AddLiftOffset(CalculateLiftDelta(rawLookY, lasso));
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
                lasso.AddLiftOffset(CalculateLiftDelta(rawLookY, lasso));
                lasso.SetVerticalAnchor(false);
                return true;
            }

            if (atMaxPitch && lookingDown)
            {
                _lassoBelowClamp = true;
                lasso.AddLiftOffset(CalculateLiftDelta(rawLookY, lasso));
                lasso.SetVerticalAnchor(false);
                return true;
            }

            if (lasso.CurrentLiftOffset != 0f)
                lasso.AddLiftOffset(-lasso.CurrentLiftOffset);

            return false;
        }
    }

    private CameraStateSettings? GetSettingsForState(CamState state)
    {
        foreach (var s in cameraStates)
            if (s.cameraState == state) return s;
        return null;
    }

    public void SetBaseXSensitivity(float value)
    {
        baseSensitivityX = value;
        cameraController.SetMouseSensitivity(baseSensitivityX, baseSensitivityY);
    }

    public void SetBaseYSensitivity(float value)
    {
        baseSensitivityY = value;
        cameraController.SetMouseSensitivity(baseSensitivityX, baseSensitivityY);
    }

    private void ApplySensitivity(float xMult, float yMult)
    {
        cameraController.SetMouseSensitivity(baseSensitivityX * xMult, baseSensitivityY * yMult);
    }
}