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



    [SerializeField] private float tetherModeAdjustSpeed;
    [SerializeField] private float lassoModeAdjustSpeed;
    [SerializeField] private float swingModeAdjustSpeed;
    [SerializeField] private float defaultAdjustSpeed;

    [Header("Default Values")]
    private float defaultCameraLens;
    private Vector3 defaultOffsetVector;

    [Header("Values Tether State")]
    [SerializeField] private float CameraLens_T;
    [SerializeField] private Vector3 camOffsetVector_T;

    [Header("Values Lasso State")]
    [SerializeField] private float CameraLens_L;
    [SerializeField] private Vector3 camOffsetVector_L;

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
    }




    public void TetherModeCamera()
    {
        cam.Lens.FieldOfView = Mathf.Lerp(cam.Lens.FieldOfView, CameraLens_T, Time.deltaTime * defaultAdjustSpeed);

        camOffset.Offset = Vector3.Lerp(camOffset.Offset, camOffsetVector_T, Time.deltaTime * defaultAdjustSpeed);
    }

    public void LassoModeCamera()
    {
        cam.Lens.FieldOfView = Mathf.Lerp(cam.Lens.FieldOfView, CameraLens_L, Time.deltaTime * defaultAdjustSpeed);

        camOffset.Offset = Vector3.Lerp(camOffset.Offset, camOffsetVector_L, Time.deltaTime * defaultAdjustSpeed);
    }

    public void SwingModeCamera()
    {
        cam.Lens.FieldOfView = Mathf.Lerp(cam.Lens.FieldOfView, CameraLens_S, Time.deltaTime * defaultAdjustSpeed);

        camOffset.Offset = Vector3.Lerp(camOffset.Offset, camOffsetVector_S, Time.deltaTime * defaultAdjustSpeed);
    }

    public void ResetCamera()
    {
        cam.Lens.FieldOfView = Mathf.Lerp(cam.Lens.FieldOfView, defaultCameraLens, Time.deltaTime * defaultAdjustSpeed);

        camOffset.Offset = Vector3.Lerp(camOffset.Offset, defaultOffsetVector, Time.deltaTime * defaultAdjustSpeed);

    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
