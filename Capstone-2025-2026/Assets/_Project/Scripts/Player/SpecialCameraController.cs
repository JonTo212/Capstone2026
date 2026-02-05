using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public enum CameraState
{
    Default,
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
    [Range(0f, 100f)] public int zPercentage;

    public CameraDirectionModifier(Vector3 dir, CameraState state, int xPercent, int yPercent, int zPercent)
    {
        direction = dir;
        cameraState = state;
        xPercentage = xPercent;
        yPercentage = yPercent;
        zPercentage = zPercent;
    }
}

public struct ScreenValues
{
    public float width;
    public float height;
    public float depth;

    public ScreenValues(float width, float height, float depth)
    {
        this.width = width;
        this.height = height;
        this.depth = depth;
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
    private Dictionary<CameraDirectionModifier, ScreenValues> directionPercentageLookup;

    [SerializeField] private float tetherModeAdjustSpeed;
    [SerializeField] private float lassoModeAdjustSpeed;
    [SerializeField] private float swingModeAdjustSpeed;
    [SerializeField] private float defaultAdjustSpeed;

    [Header("Default Values")]
    private float defaultCameraLens;
    private Vector3 defaultOffsetVector;
    public float playerXSens { get; set; }
    public float playerYSens { get; set; }

    [Header("Values Tether State")]
    [SerializeField] private float CameraLens_T;
    [SerializeField] private Vector3 camOffsetVector_T;
    [SerializeField] private float TetherSensMultiplier;

    [Header("Values Lasso State")]
    [SerializeField] private float CameraLens_L;
    [SerializeField] private Vector3 camOffsetVector_L;
    [SerializeField] private float LassoSensMultiplier;

    [Header("Values Swing State")]
    [SerializeField] private float CameraLens_S;
    [SerializeField] private Vector3 camOffsetVector_S;

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

        foreach (var c in camInput.Controllers)
        {
            if (c.Name == "Look Orbit X")
                playerXSens = c.Input.Gain;
            if (c.Name == "Look Orbit Y")
                playerYSens = c.Input.Gain;
        }

        SetUpDictionary();
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

            case LassoState.Swinging:
                SwingModeCamera();
                break;

        }
    }

    private void SetUpDictionary()
    {
        directionPercentageLookup = new Dictionary<CameraDirectionModifier, ScreenValues>();

        foreach (var entry in directionPercentages)
        {
            if (!directionPercentageLookup.ContainsKey(entry))
            {
                float xChange = entry.xPercentage / 100f;
                float yChange = entry.yPercentage / 100f;
                float zChange = entry.zPercentage / 100f;

                Vector3 dir = entry.direction.normalized;

                //int w = (int)(Screen.width + (Screen.width * xChange * dir.x));
                //int h = (int)(Screen.height + (Screen.height * yChange * dir.y));

                //converted to screen space, cinemachine is -0.5 to +0.5
                float w = xChange * dir.x * 0.5f;
                float h = yChange * -dir.y * 0.5f;
                float d = camOffset.Offset.z + (camOffset.Offset.z * zChange * dir.z);

                ScreenValues screenPos = new ScreenValues(w, h, d);
                directionPercentageLookup.Add(entry, screenPos);
            }
        }
    }

    private void ApplyCameraDirectionModifier(CameraState state, float lerpSpeed)
    {
        foreach (var kvp in directionPercentageLookup)
        {
            if (kvp.Key.cameraState == state)
            {
                ScreenValues values = kvp.Value;
                if (camRotate != null)
                {
                    Vector2 currentPos = camRotate.Composition.ScreenPosition;
                    Vector2 targetPos = Vector2.Lerp(currentPos, new Vector2(values.width, values.height), Time.deltaTime * lerpSpeed);
                    camRotate.Composition.ScreenPosition = new Vector2(targetPos.x, targetPos.y);
                }

                // Apply depth offset to camera position if needed
                // This is already handled by camOffset.Offset in your existing code
                // But if you want to use the depth from dictionary:
                // Vector3 currentOffset = camOffset.Offset;
                // Vector3 targetOffset = new Vector3(currentOffset.x, currentOffset.y, values.depthOffset);
                // camOffset.Offset = Vector3.Lerp(currentOffset, targetOffset, Time.deltaTime * lerpSpeed);

                break;
            }
        }
    }

    public void LassoModeCamera()
    {
        //cam.Lens.FieldOfView = Mathf.Lerp(cam.Lens.FieldOfView, CameraLens_L, Time.deltaTime * lassoModeAdjustSpeed);
        //camOffset.Offset = Vector3.Lerp(camOffset.Offset, camOffsetVector_L, Time.deltaTime * lassoModeAdjustSpeed);

        ApplyCameraDirectionModifier(CameraState.Lasso, lassoModeAdjustSpeed);
        ApplySensitivity(LassoSensMultiplier, 0f);
    }

    public void TetherModeCamera()
    {
        cam.Lens.FieldOfView = Mathf.Lerp(cam.Lens.FieldOfView, CameraLens_T, Time.deltaTime * tetherModeAdjustSpeed);
        camOffset.Offset = Vector3.Lerp(camOffset.Offset, camOffsetVector_T, Time.deltaTime * tetherModeAdjustSpeed);

        ApplySensitivity(TetherSensMultiplier, TetherSensMultiplier);
    }

    public void ResetCamera()
    {
        //cam.Lens.FieldOfView = Mathf.Lerp(cam.Lens.FieldOfView, defaultCameraLens, Time.deltaTime * defaultAdjustSpeed);
        //camOffset.Offset = Vector3.Lerp(camOffset.Offset, defaultOffsetVector, Time.deltaTime * defaultAdjustSpeed);

        ApplyCameraDirectionModifier(CameraState.Default, defaultAdjustSpeed);
        ApplySensitivity(1f, 1f);
    }

    public void SwingModeCamera()
    {
        cam.Lens.FieldOfView = Mathf.Lerp(cam.Lens.FieldOfView, CameraLens_S, Time.deltaTime * swingModeAdjustSpeed);
        camOffset.Offset = Vector3.Lerp(camOffset.Offset, camOffsetVector_S, Time.deltaTime * swingModeAdjustSpeed);
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
