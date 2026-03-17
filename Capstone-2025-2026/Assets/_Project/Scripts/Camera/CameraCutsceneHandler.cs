using System.Collections;
using UnityEngine;

public class CameraCutsceneHandler : MonoBehaviour
{
    public static CameraCutsceneHandler Instance { get; private set; }

    [Header("References")]
    [SerializeField] private ZeldaCameraController cameraController;
    [SerializeField] private CameraModeController cameraModeController;

    [Header("Opening Sequence")]
    [SerializeField] private CutsceneBase openingCutscene;

    private bool _isActive = false;

    public bool IsActive() => _isActive;
    public bool IsPlaying() => _cutscene != null && _cutscene.IsPlaying;

    private CutsceneBase _cutscene;

    private bool _isBlendingIn = false;
    private float _blendStartTime;
    public bool BlendingIn => _isBlendingIn;

    private bool _isBlendingOut = false;
    private float _blendOutStartTime;
    public bool BlendingOut => _isBlendingOut;

    public CutsceneBase CurrentCutscene => _cutscene;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (cameraController == null) cameraController = Camera.main?.GetComponent<ZeldaCameraController>();
        if (cameraModeController == null) cameraModeController = Camera.main?.GetComponent<CameraModeController>();
    }

    private void Start()
    {
        if (openingCutscene != null)
            StartCutscene(openingCutscene);
    }

    public void StartCutscene(CutsceneBase cutscene)
    {
        if (_isActive) return;
        if (cutscene == null) return;
        if (cutscene.IsPlaying) return;

        _cutscene = cutscene;
        StartCoroutine(CutsceneSequence());
    }

    private IEnumerator CutsceneSequence()
    {
        _isActive = true;

        _cutscene.OnCutscenePrepare();

        if (cameraController != null)
        {
            cameraController.SetXAxisLocked(true);
            cameraController.SetYAxisLocked(true);
        }

        _isBlendingIn = true;
        _cutscene.SetBlendDelayActive(true);

        if (_cutscene.BlendInDelay > 0f) yield return new WaitForSeconds(_cutscene.BlendInDelay);
        _cutscene.SetBlendDelayActive(false);

        _blendStartTime = Time.time;
        if (_cutscene.BlendInTime > 0f) yield return new WaitForSeconds(_cutscene.BlendInTime);
        _isBlendingIn = false;

        _cutscene.OnCutsceneStart();
        yield return new WaitForSeconds(_cutscene.Duration);

        if (cameraController != null)
        {
            cameraController.SetXAxisLocked(false);
            cameraController.SetYAxisLocked(false);
        }

        CutsceneBase justFinished = _cutscene;
        _cutscene = null;
        _isActive = false;

        justFinished.OnCutsceneEnd();
    }

    private void FixedUpdate()
    {
        if (!_isActive || _cutscene == null) return;

        if (_isBlendingIn)
        {
            if (!_cutscene.BlendDelayActive)
            {
                float elapsed = Time.time - _blendStartTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / _cutscene.BlendInTime));
                _cutscene.OnBlendTick(t);
            }
            return;
        }

        if (!_cutscene.IsPlaying) return;
        _cutscene.OnCutsceneTick();
    }

    private void Update()
    {
        if (!_isActive || _cutscene == null) return;

        if (_cutscene.Skippable && PlayerActions.Instance.DeactivateTetherDown)
            SkipCutscene();

        _cutscene.OnCutsceneUpdate();
    }

    private void LateUpdate()
    {
        if (!_isActive || _cutscene == null) return;

        _cutscene.OnCutsceneLateUpdate();
    }

    public void SkipCutscene()
    {
        if (!_isActive || _cutscene == null) return;

        StopAllCoroutines();

        _isBlendingIn = false;
        _isBlendingOut = false;

        if (cameraController != null)
        {
            cameraController.SetXAxisLocked(false);
            cameraController.SetYAxisLocked(false);
        }

        CutsceneBase justFinished = _cutscene;
        _cutscene = null;
        _isActive = false;

        justFinished.OnCutsceneSkip();
        justFinished.OnCutsceneEnd();
    }

    public void SetCameraPositionDamping(Vector3? damping)
    {
        cameraController?.SetPositionDamping(damping);
    }

    public void RotateCameraToDirection(Vector3 direction, float speed, float pitch)
    {
        if (cameraController == null) return;
        float targetYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        float newYaw = Mathf.LerpAngle(cameraController.GetCurrentYaw(), targetYaw, speed * Time.deltaTime);
        float newPitch = Mathf.Lerp(cameraController.GetCurrentPitch(), pitch, speed * Time.deltaTime);
        cameraController.SetRotation(newYaw, newPitch);
    }

    public void SetCameraScreenOffset(Vector2 target, float speed)
    {
        if (cameraController == null) return;
        Vector2 current = cameraController.GetScreenOffset();
        cameraController.SetScreenOffset(Vector2.Lerp(current, target, speed * Time.deltaTime));
    }

    public void SetCameraTargetOffset(Vector3 target, float speed)
    {
        if (cameraController == null) return;
        Vector3 current = cameraController.GetTargetOffset();
        cameraController.SetTargetOffset(Vector3.Lerp(current, target, speed * Time.deltaTime));
    }

    public void SetCameraScreenOffsetDirect(Vector2 offset)
    {
        cameraController?.SetScreenOffset(offset);
    }

    public void SetCameraTargetOffsetDirect(Vector3 offset)
    {
        cameraController?.SetTargetOffset(offset);
    }

    public void SetCameraZOffsetDirect(float offset)
    {
        cameraController?.SetZOffset(offset);
    }
}