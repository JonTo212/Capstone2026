using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;

public class SpecialCameraController : MonoBehaviour
{
    private CinemachineCamera cam;
    private CinemachineOrbitalFollow camFollow;
    private CinemachineRotationComposer camRotate;
    private CinemachineInputAxisController camInput;
    private CinemachineDeoccluder camCollider;
    private CinemachineCameraOffset camOffset;

    [SerializeField] private LassoTetherController lassoTetherController;

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
    }

    private void Update()
    {
        switch(lassoTetherController.CurrentLassoState)
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

    public void TetherModeCamera()
    {
        cam.Lens.FieldOfView = Mathf.Lerp(cam.Lens.FieldOfView, CameraLens_T, Time.deltaTime * tetherModeAdjustSpeed);
        camOffset.Offset = Vector3.Lerp(camOffset.Offset, camOffsetVector_T, Time.deltaTime * tetherModeAdjustSpeed);

        ApplySensitivity(TetherSensMultiplier);
    }

    public void LassoModeCamera()
    {
        cam.Lens.FieldOfView = Mathf.Lerp(cam.Lens.FieldOfView, CameraLens_L, Time.deltaTime * lassoModeAdjustSpeed);
        camOffset.Offset = Vector3.Lerp(camOffset.Offset, camOffsetVector_L, Time.deltaTime * lassoModeAdjustSpeed);

        ApplySensitivity(LassoSensMultiplier);
    }

    public void SwingModeCamera()
    {
        cam.Lens.FieldOfView = Mathf.Lerp(cam.Lens.FieldOfView, CameraLens_S, Time.deltaTime * swingModeAdjustSpeed);
        camOffset.Offset = Vector3.Lerp(camOffset.Offset, camOffsetVector_S, Time.deltaTime * swingModeAdjustSpeed);
    }

    public void ResetCamera()
    {
        cam.Lens.FieldOfView = Mathf.Lerp(cam.Lens.FieldOfView, defaultCameraLens, Time.deltaTime * defaultAdjustSpeed);
        camOffset.Offset = Vector3.Lerp(camOffset.Offset, defaultOffsetVector, Time.deltaTime * defaultAdjustSpeed);

        ApplySensitivity(1f);
    }

    public void ApplySensitivity(float multiplier)
    {
        foreach (var c in camInput.Controllers)
        {
            if (c.Name == "Look Orbit X")
                c.Input.Gain = playerXSens * multiplier;
            if (c.Name == "Look Orbit Y")
                c.Input.Gain = playerYSens * multiplier;
        }
    }
}
