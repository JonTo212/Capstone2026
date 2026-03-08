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

    // Cached values
    private Vector2 defaultScreenOffset;
    private Vector3 defaultTargetOffset;
    private float defaultDistance;
    private float characterHeight;
    private float baseSensitivityX;
    private float baseSensitivityY;
    private bool hasSnappedToLasso;
    private bool _wasInCutscene;

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

            float requiredDistanceOffset = CalculateRequiredDistanceOffset(
                playerRef.position, snaredProp.transform.position, out optimalFraming);
            currentTargetDistance = defaultDistance + requiredDistanceOffset;

            bool rotateMode = lassoTetherController.CurrentLassoState == LassoState.FreeRotating;
            ApplyCameraSettings(optimalFraming, currentTargetOffset, currentTargetDistance, true, rotateMode, lassoModeAdjustSpeed);
            cameraController.SetCollisionSmoothTimeOverride(lassoZoomOutSmoothTime);

            if (!hasSnappedToLasso)
            {
                cameraController.SetPitchSmoothOverride(lassoZoomOutSmoothTime);
                cameraController.SetRotation(cameraController.GetCurrentYaw(), 0f);
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
    }

    private void ApplyCameraStateSettings(CamState state, float smoothSpeed)
    {
        CameraStateSettings? settings = GetSettingsForState(state);
        if (settings.HasValue)
        {
            float targetDistance = defaultDistance + settings.Value.distanceOffset;
            ApplyCameraSettings(settings.Value.screenOffset, defaultTargetOffset + settings.Value.targetOffset,
                                targetDistance, settings.Value.lockYAxis, settings.Value.lockXAxis, smoothSpeed);
        }
        else
        {
            ApplyCameraSettings(defaultScreenOffset, defaultTargetOffset, defaultDistance, false, false, smoothSpeed);
        }
    }

    private void ApplyCameraSettings(Vector2 targetScreenOffset, Vector3 targetOffset, float targetDistance,
                                     bool lockY, bool lockX, float smoothSpeed)
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