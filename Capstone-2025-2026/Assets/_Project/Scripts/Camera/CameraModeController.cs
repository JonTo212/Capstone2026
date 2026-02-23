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
    [SerializeField] private float zoomPadding = 1.15f;
    [SerializeField] private float lassoZoomOutSmoothTime = 0.1f;

    private Vector2 defaultScreenOffset;
    private Vector3 defaultTargetOffset;
    private float defaultDistance;
    private float characterHeight;
    private float baseSensitivityX;
    private float baseSensitivityY;

    private Vector2 currentScreenOffset;
    private Vector3 currentTargetOffset;
    private float currentTargetDistance;
    private Vector2 optimalFraming;
    private bool hasSnappedToLasso;

    private void Start()
    {
        if (cameraController == null)
            cameraController = GetComponent<ZeldaCameraController>();

        defaultScreenOffset = cameraController.GetScreenOffset();
        defaultTargetOffset = cameraController.GetTargetOffset();
        defaultDistance = cameraController.GetDefaultDistance();

        currentScreenOffset = defaultScreenOffset;
        currentTargetOffset = defaultTargetOffset;
        currentTargetDistance = defaultDistance;

        if (playerRef != null)
        {
            var col = playerRef.GetComponent<CapsuleCollider>();
            if (col != null) characterHeight = col.height;
        }
    }

    private void Update()
    {
        if (cameraCutsceneHandler != null && cameraCutsceneHandler.IsActive())
        {
            RopeSwingCamera();
            return;
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


    private void RopeSwingCamera()
    {
        ApplyCameraStateSettings(CamState.RopeHangCutscene, defaultAdjustSpeed);
        ApplySensitivity(1f, 1f);
    }

    private void LassoModeCamera()
    {
        Prop snaredProp = lassoTetherController.Lasso.SnaredObject;
        if (snaredProp == null) { ApplySensitivity(lassoSensMultiplier, 0f); return; }

        cameraController.SetDistanceLimit(25f);

        float requiredDistanceOffset = CalculateRequiredDistanceOffset(
            playerRef.position, snaredProp.transform.position, out optimalFraming);
        currentTargetDistance = defaultDistance + requiredDistanceOffset;

        bool rotateMode = lassoTetherController.CurrentLassoState == LassoState.FreeRotating;
        ApplyCameraSettings(optimalFraming, currentTargetOffset, true, rotateMode, lassoModeAdjustSpeed);
        cameraController.SetCollisionSmoothTimeOverride(lassoZoomOutSmoothTime);

        if (!hasSnappedToLasso)
        {
            cameraController.SetPitchSmoothOverride(lassoZoomOutSmoothTime);
            cameraController.SetRotation(cameraController.GetCurrentYaw(), 0f);
            hasSnappedToLasso = true;
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

    private float CalculateRequiredDistanceOffset(Vector3 playerPos, Vector3 objectPos,
                                                   out Vector2 framing)
    {
        Camera mainCam = Camera.main;

        float playerBottom = playerPos.y - characterHeight / 2f;
        float playerTop = playerPos.y + characterHeight / 2f;
        float objectBottom = objectPos.y;
        float objectTop = objectPos.y;

        Renderer r = lassoTetherController.Lasso.SnaredObject.GetComponent<Renderer>();
        if (r != null) { objectBottom = r.bounds.min.y; objectTop = r.bounds.max.y; }

        float lowestY = Mathf.Min(playerBottom, objectBottom);
        float highestY = Mathf.Max(playerTop, objectTop);
        float verticalSpan = (highestY - lowestY) * zoomPadding;

        float fovRad = mainCam.fieldOfView * Mathf.Deg2Rad;
        float requiredDistance = Mathf.Max(verticalSpan / (2f * Mathf.Tan(fovRad / 2f)), defaultDistance);

        Vector3 midpoint = new Vector3((playerPos.x + objectPos.x) / 2f,
                                               (lowestY + highestY) / 2f,
                                               (playerPos.z + objectPos.z) / 2f);
        Vector3 midpointViewport = mainCam.WorldToViewportPoint(midpoint);

        float delta = midpointViewport.y - playerDefaultYOffset;
        float normalized = Mathf.Clamp(delta * 2f, -1f, 1f);

        framing = new Vector2(0f, normalized);
        return requiredDistance - defaultDistance;
    }

    private CameraStateSettings? GetSettingsForState(CamState state)
    {
        foreach (var s in cameraStates)
            if (s.cameraState == state) return s;
        return null;
    }

    private void ApplySensitivity(float xMult, float yMult)
    {
        cameraController.SetXSensitivity(cameraController.GetDeviceAdjustedBaseX() * xMult);
        cameraController.SetYSensitivity(cameraController.GetDeviceAdjustedBaseY() * yMult);
    }
}