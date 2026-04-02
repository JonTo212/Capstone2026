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
    public Vector2 screenOffset;        // -1 to 1
    public Vector3 worldSpaceOffset;    // world-space offset added on top of the camera's default target offset
    public float distanceOffset;        // Z distance zoom relative to default
    public bool lockYAxis;
    public bool lockXAxis;
    public float transitionSpeed;       // lerp speed when entering/leaving this state
    public float sensitivityMultiplierX;
    public float sensitivityMultiplierY;

    public CameraStateSettings(CamState state, Vector2 screenPos, Vector3 worldOff, float distOff, bool lockY, bool lockX, float speed = 6f, float sensX = 1f, float sensY = 1f)
    {
        cameraState = state;
        screenOffset = screenPos;
        worldSpaceOffset = worldOff;
        distanceOffset = distOff;
        lockYAxis = lockY;
        lockXAxis = lockX;
        transitionSpeed = speed;
        sensitivityMultiplierX = sensX;
        sensitivityMultiplierY = sensY;
    }
}

public class CameraModeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerRef;

    [Header("Camera State Settings")]
    [SerializeField] private List<CameraStateSettings> cameraStates = new();

    [Header("Lasso Mode Dynamic Zoom Settings")]
    [SerializeField] private float playerDefaultYOffset = 0.25f;
    [SerializeField] private float zoomPadding = 1.15f;
    [SerializeField] private float lassoZoomOutSmoothTime = 0.1f;

    [Header("Lasso Mode Pitch Clamp")]
    [SerializeField] private float lassoMinPitch = -30f;
    [SerializeField] private float lassoMaxPitch = 30f;

    private bool _lassoAboveClamp;
    private bool _lassoBelowClamp;

    //cached values
    private Vector2 defaultScreenOffset;
    private Vector3 defaultTargetOffset;
    private float defaultDistance;
    private float characterHeight;
    private float baseSensitivityX;
    private float baseSensitivityY;
    private bool hasSnappedToLasso;

    //framing, for lasso
    private Vector2 currentScreenOffset;
    private Vector3 currentTargetOffset;
    private float currentDistanceOffset;
    private float currentTargetDistance;
    private Vector2 optimalFraming;

    private void Awake()
    {
        defaultScreenOffset = CameraRefData.Instance.ZeldaCameraController.GetScreenOffset();
        defaultTargetOffset = CameraRefData.Instance.ZeldaCameraController.GetTargetOffset();
        defaultDistance = CameraRefData.Instance.ZeldaCameraController.GetDefaultDistance();

        currentScreenOffset = defaultScreenOffset;
        currentTargetOffset = defaultTargetOffset;
        currentDistanceOffset = 0f;
        currentTargetDistance = defaultDistance;

        if (playerRef != null)
        {
            CapsuleCollider col = playerRef.GetComponent<CapsuleCollider>();
            if (col != null) characterHeight = col.height;
        }

        baseSensitivityX = CameraRefData.Instance.ZeldaCameraController.GetMouseXSensitivity();
        baseSensitivityY = CameraRefData.Instance.ZeldaCameraController.GetMouseYSensitivity();
    }

    public (Vector2 screenOffset, Vector3 targetOffset, float distanceOffset) GetCurrentStateOffsets()
    {
        CamState state = PlayerRefData.Instance.LassoTetherController.rodEquipped ? CamState.LassoEquipped : CamState.TetherEquipped;
        CameraStateSettings? settings = GetSettingsForState(state);
        Vector2 screen = settings.HasValue ? settings.Value.screenOffset : defaultScreenOffset;
        Vector3 target = settings.HasValue ? defaultTargetOffset + settings.Value.worldSpaceOffset : defaultTargetOffset;
        float distance = settings.HasValue ? settings.Value.distanceOffset : 0f;
        return (screen, target, distance);
    }

    private void Update()
    {
        if (CameraRefData.Instance.CameraCutsceneHandler != null && CameraRefData.Instance.CameraCutsceneHandler.IsActive())
        {
            CutsceneBase current = CameraRefData.Instance.CameraCutsceneHandler.CurrentCutscene;
            if (current != null && current is PlayerCutsceneBase)
            {
                return;
            }
        }

        switch (PlayerRefData.Instance.LassoTetherController.CurrentLassoState)
        {
            case LassoState.Snared:
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

    public void ForceSnapToCurrentState()
    {
        CamState correctState = PlayerRefData.Instance.LassoTetherController.rodEquipped ? CamState.LassoEquipped : CamState.TetherEquipped;

        CameraStateSettings? settings = GetSettingsForState(correctState);

        currentScreenOffset = settings.HasValue ? settings.Value.screenOffset : defaultScreenOffset;
        currentTargetOffset = settings.HasValue ? defaultTargetOffset + settings.Value.worldSpaceOffset : defaultTargetOffset;
        currentDistanceOffset = settings.HasValue ? settings.Value.distanceOffset : 0f;

        CameraRefData.Instance.ZeldaCameraController.SetScreenOffset(currentScreenOffset);
        CameraRefData.Instance.ZeldaCameraController.SetTargetOffset(currentTargetOffset);
        CameraRefData.Instance.ZeldaCameraController.SetZOffset(currentDistanceOffset);

        float targetDistance = defaultDistance + (settings.HasValue ? settings.Value.distanceOffset : 0f);
        CameraRefData.Instance.ZeldaCameraController.SnapDistance(targetDistance);
    }

    private void LassoModeCamera()
    {
        CameraStateSettings? lassoSettings = GetSettingsForState(CamState.Lasso);
        Prop snaredProp = PlayerRefData.Instance.Lasso.SnaredObject;
        if (snaredProp != null)
        {
            PlayerRefData.Instance.Lasso.SuppressLiftInput = true;
            float requiredDistanceOffset = CalculateRequiredDistanceOffset(playerRef.position, snaredProp.transform.position, out optimalFraming);

            Vector3 lassoBaseOffset = lassoSettings.HasValue ? defaultTargetOffset + lassoSettings.Value.worldSpaceOffset : defaultTargetOffset;
            Vector2 lassoBaseScreenOffset = lassoSettings.HasValue ? lassoSettings.Value.screenOffset : defaultScreenOffset;
            float lassoBaseDistance = defaultDistance + (lassoSettings.HasValue ? lassoSettings.Value.distanceOffset : 0f);
            float lassoSpeed = lassoSettings.HasValue ? lassoSettings.Value.transitionSpeed : 6f;
            currentTargetDistance = lassoBaseDistance + requiredDistanceOffset;

            bool yLocked = HandleAboveClampInput();
            ApplyCameraSettings(lassoBaseScreenOffset + optimalFraming, lassoBaseOffset, currentTargetDistance, false, false, lassoSpeed);
            CameraRefData.Instance.ZeldaCameraController.SetCollisionSmoothTimeOverride(lassoZoomOutSmoothTime);
            CameraRefData.Instance.ZeldaCameraController.SetYAxisLocked(yLocked);
            CameraRefData.Instance.ZeldaCameraController.SetVerticalClamp(lassoMinPitch, lassoMaxPitch);

            if (!hasSnappedToLasso)
            {
                CameraRefData.Instance.ZeldaCameraController.SetPitchSmoothOverride(lassoZoomOutSmoothTime);
                float clampedPitch = Mathf.Clamp(CameraRefData.Instance.ZeldaCameraController.GetCurrentPitch(), lassoMinPitch, lassoMaxPitch);
                CameraRefData.Instance.ZeldaCameraController.SetRotation(CameraRefData.Instance.ZeldaCameraController.GetCurrentYaw(), clampedPitch, false);
                hasSnappedToLasso = true;
            }
        }

        float sensX = lassoSettings.HasValue ? lassoSettings.Value.sensitivityMultiplierX : 1f;
        float sensY = lassoSettings.HasValue ? lassoSettings.Value.sensitivityMultiplierY : 1f;
        ApplySensitivity(sensX, sensY);
    }

    private void TetherModeCamera()
    {
        CameraRefData.Instance.ZeldaCameraController.SetCollisionSmoothTimeOverride(null);
        CameraRefData.Instance.ZeldaCameraController.SetPitchSmoothOverride(null);

        if (hasSnappedToLasso)
            CameraRefData.Instance.ZeldaCameraController.SyncTargetPitchToCurrent();

        hasSnappedToLasso = false;

        CameraRefData.Instance.ZeldaCameraController.SetVerticalClamp(null, null);
        _lassoAboveClamp = false;
        _lassoBelowClamp = false;
        if (PlayerRefData.Instance.Lasso != null)
            PlayerRefData.Instance.Lasso.SuppressLiftInput = false;

        ApplyCameraStateSettings(CamState.Tether);
    }

    private void ResetCamera()
    {
        CameraRefData.Instance.ZeldaCameraController.SetCollisionSmoothTimeOverride(null);
        CameraRefData.Instance.ZeldaCameraController.SetPitchSmoothOverride(null);

        if (hasSnappedToLasso)
            CameraRefData.Instance.ZeldaCameraController.SyncTargetPitchToCurrent();

        hasSnappedToLasso = false;

        ApplyCameraStateSettings(PlayerRefData.Instance.LassoTetherController.rodEquipped ? CamState.LassoEquipped : CamState.TetherEquipped);

        currentTargetDistance = defaultDistance;
        ApplySensitivity(1f, 1f);

        CameraRefData.Instance.ZeldaCameraController.SetVerticalClamp(null, null);
        _lassoAboveClamp = false;
        _lassoBelowClamp = false;
        if (PlayerRefData.Instance.Lasso != null)
            PlayerRefData.Instance.Lasso.SuppressLiftInput = false;
    }

    private void ApplyCameraStateSettings(CamState state)
    {
        CameraStateSettings? settings = GetSettingsForState(state);
        if (settings.HasValue)
        {
            float targetDistance = defaultDistance + settings.Value.distanceOffset;
            ApplyCameraSettings(settings.Value.screenOffset, defaultTargetOffset + settings.Value.worldSpaceOffset, targetDistance, settings.Value.lockYAxis, settings.Value.lockXAxis, settings.Value.transitionSpeed);
            ApplySensitivity(settings.Value.sensitivityMultiplierX, settings.Value.sensitivityMultiplierY);
        }
        else
        {
            ApplyCameraSettings(defaultScreenOffset, defaultTargetOffset, defaultDistance, false, false, 6f);
            ApplySensitivity(1f, 1f);
        }
    }

    private void ApplyCameraSettings(Vector2 targetScreenOffset, Vector3 targetOffset, float targetDistance, bool lockY, bool lockX, float smoothSpeed)
    {
        currentScreenOffset = Vector2.Lerp(currentScreenOffset, targetScreenOffset, smoothSpeed * Time.deltaTime);
        currentTargetOffset = Vector3.Lerp(currentTargetOffset, targetOffset, smoothSpeed * Time.deltaTime);
        currentDistanceOffset = Mathf.Lerp(currentDistanceOffset, targetDistance - defaultDistance, smoothSpeed * Time.deltaTime);

        CameraRefData.Instance.ZeldaCameraController.SetScreenOffset(currentScreenOffset);
        CameraRefData.Instance.ZeldaCameraController.SetTargetOffset(currentTargetOffset);
        CameraRefData.Instance.ZeldaCameraController.SetZOffset(currentDistanceOffset);

        CameraRefData.Instance.ZeldaCameraController.SetYAxisLocked(lockY);
        CameraRefData.Instance.ZeldaCameraController.SetXAxisLocked(lockX);
    }

    private float CalculateRequiredDistanceOffset(Vector3 playerPos, Vector3 objectPos, out Vector2 optimalFraming)
    {
        Camera mainCam = Camera.main;
        float playerBottom = playerPos.y - (characterHeight / 2f);
        float playerTop = playerPos.y + (characterHeight / 2f);
        float objectBottom = objectPos.y;
        float objectTop = objectPos.y;

        Renderer objRenderer = PlayerRefData.Instance.Lasso.SnaredObject.GetComponent<Renderer>();
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
        float pitch = CameraRefData.Instance.ZeldaCameraController.GetTargetPitch();
        float newPitch = pitch - rawLookY * baseSensitivityY;
        float currentY = Mathf.Sin(pitch * Mathf.Deg2Rad) * lasso.AnchorDist;
        float newY = Mathf.Sin(newPitch * Mathf.Deg2Rad) * lasso.AnchorDist;
        return currentY - newY;
    }

    private bool HandleAboveClampInput()
    {
        Lasso lasso = PlayerRefData.Instance.Lasso;
        if (lasso == null) return false;

        float rawLookY = PlayerActions.Instance.LookInput.y;
        bool lookingUp = rawLookY > 0.001f;
        bool lookingDown = rawLookY < -0.001f;

        bool atMinPitch = CameraRefData.Instance.ZeldaCameraController.GetTargetPitch() <= lassoMinPitch + 0.01f;
        bool atMaxPitch = CameraRefData.Instance.ZeldaCameraController.GetTargetPitch() >= lassoMaxPitch - 0.01f;

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
        CameraRefData.Instance.ZeldaCameraController.SetMouseSensitivity(baseSensitivityX, baseSensitivityY);
    }

    public void SetBaseYSensitivity(float value)
    {
        baseSensitivityY = value;
        CameraRefData.Instance.ZeldaCameraController.SetMouseSensitivity(baseSensitivityX, baseSensitivityY);
    }

    private void ApplySensitivity(float xMult, float yMult)
    {
        CameraRefData.Instance.ZeldaCameraController.SetMouseSensitivity(baseSensitivityX * xMult, baseSensitivityY * yMult);
    }
}