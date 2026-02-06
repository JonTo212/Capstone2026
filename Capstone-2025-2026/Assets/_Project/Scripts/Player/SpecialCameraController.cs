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
    [Range(60f, 100f)] public int FOV;

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

    [SerializeField] private Transform characterTransform;
    [SerializeField] private float characterApproximateRadius = 0.5f;
    [SerializeField] private float safetyMargin = 0.05f; //in percentage, from the screen edge

    [Header("Default Values")]
    private float defaultCameraLens;
    private Vector3 defaultOffsetVector;
    private Vector2 defaultScreenPosition;
    public float playerXSens { get; set; }
    public float playerYSens { get; set; }

    [Header("Values Tether State")]
    [SerializeField] private float CameraLens_T;
    [SerializeField] private float TetherSensMultiplier;

    [Header("Values Lasso State")]
    [SerializeField] private float CameraLens_L;
    [SerializeField] private float LassoSensMultiplier;

    private void Awake()
    {
        cam = GetComponent<CinemachineCamera>();
        camFollow = GetComponent<CinemachineOrbitalFollow>();
        camRotate = GetComponent<CinemachineRotationComposer>();
        camInput = GetComponent<CinemachineInputAxisController>();
        camCollider = GetComponent<CinemachineDeoccluder>();
        camOffset = GetComponent<CinemachineCameraOffset>();

        defaultCameraLens = cam.Lens.FieldOfView;
        defaultOffsetVector = camOffset.Offset;
        defaultScreenPosition = camRotate != null ? camRotate.Composition.ScreenPosition : Vector2.zero;

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
        float zPercent = modifier.FOV / 100f;

        Vector3 dir = modifier.direction;
        float xDir = Mathf.Sign(dir.x); // -1 for left, +1 for right
        float yDir = Mathf.Sign(dir.y); // -1 for down, +1 for up
        float zDir = Mathf.Sign(dir.z); // -1 for closer, +1 for farther

        //character stats
        Vector3 characterWorldPos = characterTransform.position;
        Vector3 screenCenter = Camera.main.WorldToViewportPoint(characterWorldPos) + camOffset.Offset;
        Vector3 characterEdge = Camera.main.WorldToViewportPoint(characterWorldPos + Camera.main.transform.right * characterApproximateRadius);
        float characterScreenRadius = Mathf.Abs(characterEdge.x - screenCenter.x);

        //max distance is between -0.5 and +0.5, minus character radius and safety margin
        float maxSafeX = 0.5f - characterScreenRadius - safetyMargin;
        float maxSafeY = 0.5f - characterScreenRadius - safetyMargin;

        //apply the percentage to the maximum safe offset
        float xOffset = xPercent * maxSafeX * xDir;
        float yOffset = yPercent * maxSafeY * yDir;
        float zOffset = defaultOffsetVector.z * (1f + (zPercent * zDir));

        return new ScreenValues(xOffset, yOffset, zOffset);
    }

    private void ApplyCameraDirectionModifier(CameraState state, float lerpSpeed)
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
                Vector2 targetPos = Vector2.Lerp(currentPos, defaultScreenPosition, Time.deltaTime * lerpSpeed);
                camRotate.Composition.ScreenPosition = targetPos;
            }
            return;
        }

        ScreenValues values = CalculateScreenOffsetForDirection(activeModifier.Value);
        if (camRotate != null)
        {
            Vector2 currentPos = camRotate.Composition.ScreenPosition;
            Vector2 targetPos = new Vector2(values.xOffset, values.yOffset);
            camRotate.Composition.ScreenPosition = Vector2.Lerp(currentPos, targetPos, Time.deltaTime * lerpSpeed);
        }
    }

    public void LassoModeCamera()
    {
        cam.Lens.FieldOfView = Mathf.Lerp(cam.Lens.FieldOfView, CameraLens_L, Time.deltaTime * lassoModeAdjustSpeed);

        ApplyCameraDirectionModifier(CameraState.Lasso, lassoModeAdjustSpeed);
        ApplySensitivity(LassoSensMultiplier, 0f);
    }

    public void TetherModeCamera()
    {
        cam.Lens.FieldOfView = Mathf.Lerp(cam.Lens.FieldOfView, CameraLens_T, Time.deltaTime * tetherModeAdjustSpeed);

        ApplyCameraDirectionModifier(CameraState.Tether, tetherModeAdjustSpeed);
        ApplySensitivity(TetherSensMultiplier, TetherSensMultiplier);
    }

    public void ResetCamera()
    {
        cam.Lens.FieldOfView = Mathf.Lerp(cam.Lens.FieldOfView, defaultCameraLens, Time.deltaTime * defaultAdjustSpeed);

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