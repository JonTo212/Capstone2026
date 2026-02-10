using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public enum CamState
{
    LassoEquipped,
    TetherEquipped,
    Lasso,
    Tether,
    LedgeGrab,
    HighSpeed
}

[System.Serializable]
public struct CameraDirectionModifier
{
    public Vector3 direction;
    public CamState cameraState;
    [Range(0f, 100f)] public int xPercentage;
    [Range(0f, 100f)] public int yPercentage;
    public Vector3 camOffset;
    public int FOV;

    public CameraDirectionModifier(Vector3 dir, CamState state, int xPercent, int yPercent, Vector3 offset, int desiredFOV)
    {
        direction = dir;
        cameraState = state;
        xPercentage = xPercent;
        yPercentage = yPercent;
        camOffset = offset;
        FOV = desiredFOV;
    }
}

public struct ScreenValues
{
    public float xOffset;
    public float yOffset;
    public Vector3 camOffset;
    public float FOV;

    public ScreenValues(float x, float y, Vector3 offset, float fov)
    {
        xOffset = x;
        yOffset = y;
        camOffset = offset;
        FOV = fov;
    }
}


public class SpecialCameraController : MonoBehaviour
{
    private CinemachineCamera cam;
    private CinemachineOrbitalFollow camFollow;
    private CinemachineRotationComposer camRotate;
    private CinemachineInputAxisController camInput;
    private CinemachineDeoccluder camCollider;
    private CinemachineCameraOffset camOffset;

    [SerializeField] private LassoTetherController lassoTetherController;
    [SerializeField] private List<CameraDirectionModifier> directionPercentages = new();

    [SerializeField] private float tetherModeAdjustSpeed;
    [SerializeField] private float lassoModeAdjustSpeed;
    [SerializeField] private float defaultAdjustSpeed;

    [SerializeField] private Transform playerRef;
    [SerializeField] private float safetyMargin = 0.05f; //in percentage, from the screen edge
    private float characterHeight;
    private float characterRadius;

    [Header("Default Values")]
    private float defaultCameraLens;
    private Vector2 defaultScreenPosition;
    private Vector3 defaultCameraOffset;
    public float playerXSens { get; set; }
    public float playerYSens { get; set; }

    [SerializeField] private float TetherSensMultiplier;
    [SerializeField] private float LassoSensMultiplier;

    private Vector2 screenPosVelocity;

    [Header("Lasso Mode Dynamic FOV Settings")]
    [SerializeField] private float fovZoomSpeed = 8f;
    [SerializeField] private float baseMidpointOffset = 0.05f;
    [SerializeField] private float maxMidpointOffset = 0.1f;
    [SerializeField] private float playerDefaultYOffset = 0.25f;
    [SerializeField] private float fovPadding = 1.15f; 

    private float currentTargetFOV;

    private void Awake()
    {
        cam = GetComponent<CinemachineCamera>();
        camFollow = GetComponent<CinemachineOrbitalFollow>();
        camRotate = GetComponent<CinemachineRotationComposer>();
        camInput = GetComponent<CinemachineInputAxisController>();
        camCollider = GetComponent<CinemachineDeoccluder>();
        camOffset = GetComponent<CinemachineCameraOffset>();

        characterHeight = playerRef.GetComponent<CapsuleCollider>().height;
        characterRadius = playerRef.GetComponent<CapsuleCollider>().radius;

        defaultCameraLens = cam.Lens.FieldOfView;
        defaultScreenPosition = camRotate.Composition.ScreenPosition;
        defaultCameraOffset = camOffset.Offset;
        currentTargetFOV = defaultCameraLens;

        foreach (var c in camInput.Controllers)
        {
            if (c.Name == "Look Orbit X")
                playerXSens = c.Input.Gain;
            if (c.Name == "Look Orbit Y")
                playerYSens = c.Input.Gain;
        }
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

    private ScreenValues CalculateScreenOffsetForDirection(CameraDirectionModifier modifier)
    {
        float xPercent = modifier.xPercentage / 100f;
        float yPercent = modifier.yPercentage / 100f;

        Vector3 dir = modifier.direction;
        float xDir = Mathf.Sign(dir.x); // -1 for left, +1 for right
        float yDir = Mathf.Sign(dir.y); // -1 for down, +1 for up

        //character stats
        Vector3 characterWorldPos = playerRef.position;
        Vector3 screenCenter = Camera.main.WorldToViewportPoint(characterWorldPos) + camOffset.Offset;
        Vector3 characterEdge = Camera.main.WorldToViewportPoint(characterWorldPos + Camera.main.transform.right * characterRadius);
        float characterScreenRadius = Mathf.Abs(characterEdge.x - screenCenter.x);
        float characterScreenHeight = Mathf.Abs(characterEdge.y - screenCenter.y);

        //max distance is between -0.5 and +0.5, minus character radius and safety margin
        float maxSafeX = 0.5f - characterScreenRadius - safetyMargin;
        float maxSafeY = 0.5f - characterScreenRadius - safetyMargin;

        //apply the percentage to the maximum safe offset
        float xOffset = xPercent * maxSafeX * xDir;
        float yOffset = yPercent * maxSafeY * yDir;

        return new ScreenValues(xOffset, yOffset, modifier.camOffset, modifier.FOV);
    }

    private void ApplyCameraDirectionModifier(CamState state, float smoothSpeed)
    {
        CameraDirectionModifier? activeModifier = null;
        foreach (var modifier in directionPercentages)
        {
            if (modifier.cameraState == state)
            {
                activeModifier = modifier;
                break;
            }
        }

        //fallback
        if (!activeModifier.HasValue)
        {
            if (camRotate != null)
            {
                Vector2 currentPos = camRotate.Composition.ScreenPosition;
                Vector2 targetPos = Vector2.Lerp(currentPos, defaultScreenPosition, smoothSpeed * Time.deltaTime);
                camRotate.Composition.ScreenPosition = targetPos;
                cam.Lens.FieldOfView = Mathf.Lerp(cam.Lens.FieldOfView, defaultCameraLens, smoothSpeed * Time.deltaTime);
                camOffset.Offset = Vector3.Lerp(camOffset.Offset, defaultCameraOffset, smoothSpeed * Time.deltaTime);
                //camFollow.TargetOffset = Vector3.Lerp(camFollow.TargetOffset, Vector3.zero, smoothSpeed * Time.deltaTime);
            }
            return;
        }

        ScreenValues values = CalculateScreenOffsetForDirection(activeModifier.Value);
        if (camRotate != null)
        {
            Vector2 currentPos = camRotate.Composition.ScreenPosition;
            Vector2 targetPos = new Vector2(values.xOffset, values.yOffset);
            camRotate.Composition.ScreenPosition = Vector2.Lerp(currentPos, targetPos, smoothSpeed * Time.deltaTime);
            cam.Lens.FieldOfView = Mathf.Lerp(cam.Lens.FieldOfView, values.FOV, smoothSpeed * Time.deltaTime);
            camOffset.Offset = Vector3.Lerp(camOffset.Offset, values.camOffset, smoothSpeed * Time.deltaTime);
            //camFollow.TargetOffset = Vector3.Lerp(camFollow.TargetOffset, Vector3.zero, smoothSpeed * Time.deltaTime);
        }
    }

    private void LockCameraToCenter(float smoothSpeed)
    {
        if (camRotate != null)
        {
            camRotate.Composition.ScreenPosition = Vector2.SmoothDamp(camRotate.Composition.ScreenPosition, Vector2.zero, ref screenPosVelocity, camFollow.VerticalAxis.Recentering.Time);
            camFollow.VerticalAxis.Value = Mathf.Lerp(camFollow.VerticalAxis.Value, camFollow.VerticalAxis.Center, smoothSpeed * Time.deltaTime);
            camOffset.Offset = Vector3.Lerp(camOffset.Offset, new Vector3(0, 2, 2), smoothSpeed * Time.deltaTime);
            //camFollow.VerticalAxis.TriggerRecentering();
        }
    }

    private void DisableCameraInput()
    {
        if (camInput != null && camInput.enabled)
        {
            foreach (var c in camInput.Controllers)
            {
                if (c.Name == "Look Orbit Y")
                    c.Enabled = false;
            }
        }
    }

    private void EnableCameraInput()
    {
        if (camInput != null)
        {
            foreach (var c in camInput.Controllers)
            {
                c.Enabled = true;
            }
        }
    }

    private float CalculateRequiredFOV(Vector3 playerPos, Vector3 objectPos, out Vector2 optimalFraming)
    {
        //positions of player and object relative to viewport
        Vector3 playerViewport = Camera.main.WorldToViewportPoint(playerPos);
        Vector3 objectViewport = Camera.main.WorldToViewportPoint(objectPos);

        //player size
        Vector3 playerTopEdge = Camera.main.WorldToViewportPoint(playerPos + Vector3.up * (characterHeight / 2f));
        float playerScreenHeight = Mathf.Abs(playerTopEdge.y - playerViewport.y);

        //object size, using bounds as an approximation
        float objectScreenHeight = 0f;
        Vector3 objectSize = Vector3.zero;

        if (lassoTetherController.Lasso.SnaredObject != null)
        {
            Renderer objectRenderer = lassoTetherController.Lasso.SnaredObject.GetComponent<Renderer>();
            if (objectRenderer != null)
            {
                Bounds bounds = objectRenderer.bounds;
                objectSize = bounds.extents;
                Vector3 objTopEdge = Camera.main.WorldToViewportPoint(objectPos + Vector3.up * objectSize.y);
                objectScreenHeight = Mathf.Abs(objTopEdge.y - objectViewport.y);
            }
        }

        //world space positions for player feet and object top/bottom
        float playerGroundY = playerPos.y - (characterHeight / 2f);
        float playerTopY = playerPos.y + (characterHeight / 2f);
        float objectBottomY = objectPos.y - objectSize.y;
        float objectTopY = objectPos.y + objectSize.y;

        //lowest point is player feet or object bottom, highest is top of player or object
        //vertical span = how much needs to be shown on Y axis
        float lowestPoint = Mathf.Min(playerGroundY, objectBottomY);
        float highestPoint = Mathf.Max(playerTopY, objectTopY);
        float verticalSpan = highestPoint - lowestPoint;

        //calculate dynamic offset based on actual position of object relative to the player's feet
        float relativeObjectHeight = objectPos.y - playerGroundY;
        float minLiftHeight = lassoTetherController.Lasso.minLiftHeight;
        float maxLiftHeight = lassoTetherController.Lasso.maxLiftHeight;
        float objectHeightAboveGround = objectPos.y - playerGroundY;
        float heightRatio = Mathf.InverseLerp(minLiftHeight, maxLiftHeight, objectHeightAboveGround);

        //midpoint determines where camera aims
        Vector3 midpoint = new Vector3((playerPos.x + objectPos.x) / 2f, (lowestPoint + highestPoint) / 2f, (playerPos.z + objectPos.z) / 2f);
        float currentMidpointOffset = Mathf.Lerp(baseMidpointOffset, maxMidpointOffset, heightRatio);
        midpoint.y -= verticalSpan * currentMidpointOffset;


        //use inverse tangent to get angle for viewport
        float distanceToMidpoint = Vector3.Distance(Camera.main.transform.position, midpoint);
        float halfSpanWithMargin = (verticalSpan / 2f) * fovPadding;
        float requiredFOV = 2f * Mathf.Atan(halfSpanWithMargin / distanceToMidpoint) * Mathf.Rad2Deg;

        //calculate horizontal FOV
        float horizontalSpan = Vector3.Distance(new Vector3(playerPos.x, 0, playerPos.z), new Vector3(objectPos.x, 0, objectPos.z));
        horizontalSpan += characterRadius * 2f + objectSize.x * 2f;
        float aspectRatio = Camera.main.aspect;
        float horizontalFOV = 2f * Mathf.Atan((horizontalSpan / 2f * fovPadding) / distanceToMidpoint) * Mathf.Rad2Deg;
        float verticalFOVFromHorizontal = horizontalFOV / aspectRatio;

        //use larger of the vertical vs horizontal FOVs to ensure everything fits properly
        //clamped to ensure it doesn't get too small/big
        requiredFOV = Mathf.Max(requiredFOV, verticalFOVFromHorizontal);
        requiredFOV = Mathf.Clamp(requiredFOV, defaultCameraLens * 0.9f, defaultCameraLens * 1.8f);

        Vector3 midpointViewport = Camera.main.WorldToViewportPoint(midpoint);

        //0.15 is 15% of the viewport -> minimum place the player will sit
        float zoomRatio = requiredFOV / defaultCameraLens;
        float targetPlayerYOffset = Mathf.Lerp(playerDefaultYOffset, 0.15f, (zoomRatio - 1f) / 0.8f);
        float yOffset = midpointViewport.y - 0.5f;

        //keep player from dropping below target offset
        float playerTargetViewportY = -0.5f + targetPlayerYOffset + playerScreenHeight + safetyMargin;
        float currentPlayerY = playerViewport.y - 0.5f;

        if (currentPlayerY - yOffset < playerTargetViewportY)
        {
            yOffset = currentPlayerY - playerTargetViewportY;
        }

        float maxSafeY = 0.5f - Mathf.Max(playerScreenHeight, objectScreenHeight) - safetyMargin;
        yOffset = Mathf.Clamp(yOffset, -maxSafeY, maxSafeY);

        optimalFraming = new Vector2(0, yOffset);
        return requiredFOV;
    }

    private Vector2 CalculateMidpoint(Vector3 playerPos, Vector3 heldObjectPos)
    {
        Vector3 midpoint = (playerPos + heldObjectPos) / 2f;

        // Convert to viewport space
        Vector3 playerViewport = Camera.main.WorldToViewportPoint(playerPos);
        Vector3 objectViewport = Camera.main.WorldToViewportPoint(heldObjectPos);
        Vector3 targetViewport = Camera.main.WorldToViewportPoint(midpoint);

        Vector3 playerTopEdge = Camera.main.WorldToViewportPoint(playerPos + Vector3.up * (characterHeight / 2f));
        float playerScreenHeight = Mathf.Abs(playerTopEdge.y - playerViewport.y);
        float yOffset = targetViewport.y - 0.5f;

        float objectScreenHeight = 0f;
        if (lassoTetherController.Lasso.SnaredObject != null)
        {
            Renderer objectRenderer = lassoTetherController.Lasso.SnaredObject.GetComponent<Renderer>();
            if (objectRenderer != null)
            {
                Bounds bounds = objectRenderer.bounds;
                Vector3 objectSize = bounds.extents; // extents are half-size

                Vector3 objRightEdge = Camera.main.WorldToViewportPoint(heldObjectPos + Camera.main.transform.right * objectSize.x);
                Vector3 objTopEdge = Camera.main.WorldToViewportPoint(heldObjectPos + Vector3.up * objectSize.y);
                objectScreenHeight = Mathf.Abs(objTopEdge.y - objectViewport.y);
            }
        }

        float maxScreenHeight = Mathf.Max(playerScreenHeight, objectScreenHeight);
        float maxSafeY = 0.5f - maxScreenHeight - safetyMargin;
        yOffset = Mathf.Clamp(yOffset, -maxSafeY, maxSafeY);

        return new Vector2(0, yOffset);
    }

    public void LassoModeCamera()
    {
        LockCameraToCenter(lassoModeAdjustSpeed);
        DisableCameraInput();

        Prop snaredProp = lassoTetherController.Lasso.SnaredObject;
        if (snaredProp != null && !snaredProp.IsTetherPulled)
        {
            Vector3 playerPos = playerRef.position;
            Vector3 heldObjectPos = lassoTetherController.Lasso.SnaredObject.transform.position;

            //dynamic fov stuff that isn't working great
            Vector2 optimalFraming;
            float requiredFOV = CalculateRequiredFOV(playerPos, heldObjectPos, out optimalFraming);
            currentTargetFOV = requiredFOV;

            //Vector2 dynamicScreenPos = CalculateMidpoint(playerPos, heldObjectPos); //old fov stuff that was also kinda mid

            if (camRotate != null)
            {
                Vector2 currentPos = camRotate.Composition.ScreenPosition;
                camRotate.Composition.ScreenPosition = Vector2.Lerp(currentPos, optimalFraming, Time.deltaTime * lassoModeAdjustSpeed);
            }

            cam.Lens.FieldOfView = Mathf.Lerp(cam.Lens.FieldOfView, currentTargetFOV, Time.deltaTime * fovZoomSpeed);
            ApplyCameraDirectionModifier(CamState.Lasso, lassoModeAdjustSpeed);
        }
        else
        {
            //nothing held (or object is tethered), use default
            ApplyCameraDirectionModifier(CamState.Lasso, lassoModeAdjustSpeed);
            currentTargetFOV = defaultCameraLens;
            cam.Lens.FieldOfView = Mathf.Lerp(cam.Lens.FieldOfView, defaultCameraLens, Time.deltaTime * fovZoomSpeed);
        }
        ApplySensitivity(LassoSensMultiplier, 0f);
    }

    public void TetherModeCamera()
    {
        EnableCameraInput();
        ApplyCameraDirectionModifier(CamState.Tether, tetherModeAdjustSpeed);
        ApplySensitivity(TetherSensMultiplier, TetherSensMultiplier);
    }

    public void ResetCamera()
    {
        EnableCameraInput();

        if (lassoTetherController.rodEquipped) ApplyCameraDirectionModifier(CamState.LassoEquipped, defaultAdjustSpeed);
        else ApplyCameraDirectionModifier(CamState.TetherEquipped, defaultAdjustSpeed);

        // Reset FOV to default
        currentTargetFOV = defaultCameraLens;
        cam.Lens.FieldOfView = Mathf.Lerp(cam.Lens.FieldOfView, defaultCameraLens, Time.deltaTime * fovZoomSpeed);

        ApplySensitivity(1f, 1f);
    }

    public void ApplySensitivity(float xMult, float yMult)
    {
        foreach (var c in camInput.Controllers)
        {
            if (c.Name == "Look Orbit X")
                c.Input.Gain = playerXSens * xMult;
            if (c.Name == "Look Orbit Y")
                c.Input.Gain = playerYSens * yMult;
        }
    }
}