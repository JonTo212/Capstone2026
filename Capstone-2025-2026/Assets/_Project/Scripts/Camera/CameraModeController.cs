using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct CameraStateSettings
{
    public CamState cameraState;
    public Vector2 screenOffset; // -1 to 1
    public Vector3 targetOffset; // World-space
    public float distanceOffset; // Zoom relative to default
    public bool lockYAxis;

    public CameraStateSettings(CamState state, Vector2 screenPos, Vector3 targetOff, float distOff, bool lockY)
    {
        cameraState = state;
        screenOffset = screenPos;
        targetOffset = targetOff;
        distanceOffset = distOff;
        lockYAxis = lockY;
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

    // Cached values
    private Vector2 defaultScreenOffset;
    private Vector3 defaultTargetOffset;
    private float defaultDistance;
    private float characterHeight;
    private float baseSensitivityX;
    private float baseSensitivityY;
    private bool hasSnappedToLasso;


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
            CapsuleCollider collider = playerRef.GetComponent<CapsuleCollider>();
            if (collider != null)
            {
                characterHeight = collider.height;
            }
        }

        baseSensitivityX = cameraController.GetMouseXSensitivity();
        baseSensitivityY = cameraController.GetMouseYSensitivity();
    }

    private void Update()
    {
        switch (lassoTetherController.CurrentLassoState)
        {
            case LassoState.Snared:
                LassoModeCamera();
                break;

            case LassoState.Empty:
                ResetCamera();
                break;

            case LassoState.Tethering:
                TetherModeCamera();
                break;

            case LassoState.SnaredTether:
                TetherModeCamera();
                break;
        }
    }

    private void LassoModeCamera()
    {
        Prop snaredProp = lassoTetherController.Lasso.SnaredObject;
        if (snaredProp != null && !snaredProp.IsTetherPulled)
        {
            //more zoom outwards in lasso mode
            cameraController.SetDistanceLimit(25f);

            Vector3 playerPos = playerRef.position;
            Vector3 heldObjectPos = snaredProp.transform.position;

            float requiredDistanceOffset = CalculateRequiredDistanceOffset(playerPos, heldObjectPos, out optimalFraming);
            currentTargetDistance = defaultDistance + requiredDistanceOffset;

            ApplyCameraSettings(optimalFraming, currentTargetOffset, currentTargetDistance, true, lassoModeAdjustSpeed);
            cameraController.SetCollisionSmoothTimeOverride(0.1f);

            if (!hasSnappedToLasso)
            {
                cameraController.SetPitchSmoothOverride(0.1f);
                cameraController.SetRotation(cameraController.GetCurrentYaw(), 0f);

                hasSnappedToLasso = true;
            }
        }

        ApplySensitivity(lassoSensMultiplier, 0f);
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
        cameraController.SetDistanceLimit(10f);
        hasSnappedToLasso = false;

        if (lassoTetherController.rodEquipped)
            ApplyCameraStateSettings(CamState.LassoEquipped, defaultAdjustSpeed);
        else
            ApplyCameraStateSettings(CamState.TetherEquipped, defaultAdjustSpeed);

        currentTargetDistance = defaultDistance;
        ApplySensitivity(1f, 1f);
    }

    private void ApplyCameraStateSettings(CamState state, float smoothSpeed)
    {
        CameraStateSettings? settings = GetSettingsForState(state);

        if (settings.HasValue)
        {
            CameraStateSettings camSettings = settings.Value;
            float targetDistance = defaultDistance + camSettings.distanceOffset;

            ApplyCameraSettings(camSettings.screenOffset, defaultTargetOffset + camSettings.targetOffset, targetDistance, camSettings.lockYAxis, smoothSpeed);
        }
        else
        {
            ApplyCameraSettings(defaultScreenOffset, defaultTargetOffset, defaultDistance, false, smoothSpeed);
        }
    }

    private void ApplyCameraSettings(Vector2 targetScreenOffset, Vector3 targetOffset, float targetDistance, bool lockY, float smoothSpeed)
    {
        currentScreenOffset = Vector2.Lerp(currentScreenOffset, targetScreenOffset, smoothSpeed * Time.deltaTime);
        currentTargetOffset = Vector3.Lerp(currentTargetOffset, targetOffset, smoothSpeed * Time.deltaTime);
        currentDistanceOffset = Mathf.Lerp(currentDistanceOffset, targetDistance - defaultDistance, smoothSpeed * Time.deltaTime);

        cameraController.SetScreenOffset(currentScreenOffset);
        cameraController.SetTargetOffset(currentTargetOffset);
        cameraController.SetDistance(defaultDistance + currentDistanceOffset);

        cameraController.SetYAxisLocked(lockY);
    }

    private float CalculateRequiredDistanceOffset(Vector3 playerPos, Vector3 objectPos, out Vector2 optimalFraming)
    {
        Camera mainCam = Camera.main;

        //player and object sizes
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
        float verticalSpan = highestY - lowestY;

        //add zoom padding
        verticalSpan *= zoomPadding;

        float fovRad = mainCam.fieldOfView * Mathf.Deg2Rad;
        float requiredDistance = verticalSpan / (2f * Mathf.Tan(fovRad / 2f));
        requiredDistance = Mathf.Max(requiredDistance, defaultDistance);

        //midpoint between player and object
        Vector3 worldMidpoint = new Vector3((playerPos.x + objectPos.x) / 2f, (lowestY + highestY) / 2f, (playerPos.z + objectPos.z) / 2f);
        Vector3 midpointViewport = mainCam.WorldToViewportPoint(worldMidpoint);

        //player's default position, inc offset
        float currentY = midpointViewport.y;
        float targetY = playerDefaultYOffset;
        float delta = currentY - targetY;
        float normalized = Mathf.Clamp(delta * 2f, -1f, 1f);

        optimalFraming = new Vector2(0f, normalized);
        return requiredDistance - defaultDistance;
    }


    private CameraStateSettings? GetSettingsForState(CamState state)
    {
        foreach (var settings in cameraStates)
        {
            if (settings.cameraState == state) return settings;
        }
        return null;
    }

    private void ApplySensitivity(float xMult, float yMult)
    {
        cameraController.SetMouseSensitivity(baseSensitivityX * xMult, baseSensitivityY * yMult);
    }
}