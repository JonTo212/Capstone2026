using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public enum CameraState
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
    public CameraState cameraState;
    [Range(0f, 100f)] public int xPercentage;
    [Range(0f, 100f)] public int yPercentage;
    public int FOV;

    public CameraDirectionModifier(Vector3 dir, CameraState state, int xPercent, int yPercent, int desiredFOV)
    {
        direction = dir;
        cameraState = state;
        xPercentage = xPercent;
        yPercentage = yPercent;
        FOV = desiredFOV;
    }
}

public struct ScreenValues
{
    public float xOffset;
    public float yOffset;
    public float FOV;

    public ScreenValues(float x, float y, float z)
    {
        xOffset = x;
        yOffset = y;
        FOV = z;
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
    public float playerXSens { get; set; }
    public float playerYSens { get; set; }

    [SerializeField] private float TetherSensMultiplier;
    [SerializeField] private float LassoSensMultiplier;
    
    private Vector2 screenPosVelocity;

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

        //max distance is between -0.5 and +0.5, minus character radius and safety margin
        float maxSafeX = 0.5f - characterScreenRadius - safetyMargin;
        float maxSafeY = 0.5f - characterScreenRadius - safetyMargin;

        //apply the percentage to the maximum safe offset
        float xOffset = xPercent * maxSafeX * xDir;
        float yOffset = yPercent * maxSafeY * yDir;

        return new ScreenValues(xOffset, yOffset, modifier.FOV);
    }

    private void ApplyCameraDirectionModifier(CameraState state, float smoothSpeed)
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
                camFollow.TargetOffset = Vector3.Lerp(camFollow.TargetOffset, Vector3.zero, smoothSpeed * Time.deltaTime);
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
            camFollow.TargetOffset = Vector3.Lerp(camFollow.TargetOffset, Vector3.zero, smoothSpeed * Time.deltaTime);
        }
    }

    private void LockCameraToCenter(float smoothSpeed)
    {
        if (camRotate != null)
        {
            camRotate.Composition.ScreenPosition = Vector2.SmoothDamp(camRotate.Composition.ScreenPosition, Vector2.zero, ref screenPosVelocity, camFollow.VerticalAxis.Recentering.Time);
            camFollow.VerticalAxis.Value = Mathf.Lerp(camFollow.VerticalAxis.Value, camFollow.VerticalAxis.Center, smoothSpeed * Time.deltaTime);
            camFollow.TargetOffset = Vector3.Lerp(camFollow.TargetOffset, new Vector3(0, 2, 0), smoothSpeed * Time.deltaTime);
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

        if (lassoTetherController.Lasso.SnaredObject != null)
        {
            Vector3 playerPos = playerRef.position;
            Vector3 heldObjectPos = lassoTetherController.Lasso.SnaredObject.transform.position;

            Vector2 dynamicScreenPos = CalculateMidpoint(playerPos, heldObjectPos);

            if (camRotate != null)
            {
                Vector2 currentPos = camRotate.Composition.ScreenPosition;
                camRotate.Composition.ScreenPosition = Vector2.Lerp(currentPos, dynamicScreenPos, Time.deltaTime * lassoModeAdjustSpeed);
            }
            ApplyCameraDirectionModifier(CameraState.Lasso, lassoModeAdjustSpeed);
        }
        else
        {
            ApplyCameraDirectionModifier(CameraState.Lasso, lassoModeAdjustSpeed);
        }
        ApplySensitivity(LassoSensMultiplier, 0f);
    }

    public void TetherModeCamera()
    {
        ApplyCameraDirectionModifier(CameraState.Tether, tetherModeAdjustSpeed);
        ApplySensitivity(TetherSensMultiplier, TetherSensMultiplier);
    }

    public void ResetCamera()
    {
        EnableCameraInput();

        if (lassoTetherController.rodEquipped) ApplyCameraDirectionModifier(CameraState.LassoEquipped, defaultAdjustSpeed);
        else ApplyCameraDirectionModifier(CameraState.TetherEquipped, defaultAdjustSpeed);

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