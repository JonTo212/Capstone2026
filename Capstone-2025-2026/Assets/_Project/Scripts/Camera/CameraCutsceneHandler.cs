using System.Collections;
using UnityEngine;

public class CameraCutsceneHandler : MonoBehaviour
{
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

        CameraRefData.Instance.ZeldaCameraController.SetXAxisLocked(true);
        CameraRefData.Instance.ZeldaCameraController.SetYAxisLocked(true);

        _isBlendingIn = true;
        _cutscene.SetBlendDelayActive(true);

        if (_cutscene.BlendInDelay > 0f) yield return new WaitForSeconds(_cutscene.BlendInDelay);
        _cutscene.SetBlendDelayActive(false);

        _blendStartTime = Time.time;
        if (_cutscene.BlendInTime > 0f) yield return new WaitForSeconds(_cutscene.BlendInTime);
        _isBlendingIn = false;

        _cutscene.OnCutsceneStart();

        if (_cutscene.Indefinite) yield return new WaitUntil(() => _cutscene.SkipInputDetected);
        else yield return new WaitForSeconds(_cutscene.Duration);

        CameraRefData.Instance.ZeldaCameraController.SetXAxisLocked(false);
        CameraRefData.Instance.ZeldaCameraController.SetYAxisLocked(false);

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

        CameraRefData.Instance.ZeldaCameraController.SetXAxisLocked(false);
        CameraRefData.Instance.ZeldaCameraController.SetYAxisLocked(false);

        CutsceneBase justFinished = _cutscene;
        _cutscene = null;
        _isActive = false;

        justFinished.OnCutsceneSkip();
        justFinished.OnCutsceneEnd();
    }

    public void SetCameraPositionDamping(Vector3? damping)
    {
        CameraRefData.Instance.ZeldaCameraController?.SetPositionDamping(damping);
    }

    public void SetRotationDirect(Vector3 direction, float pitch)
    {
        float targetYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        CameraRefData.Instance.ZeldaCameraController.SetRotation(targetYaw, pitch, true);
    }

    public void RotateCameraToDirection(Vector3 direction, float speed, float pitch)
    {
        float targetYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        float newYaw = Mathf.LerpAngle(CameraRefData.Instance.ZeldaCameraController.GetCurrentYaw(), targetYaw, speed * Time.deltaTime);
        float newPitch = Mathf.Lerp(CameraRefData.Instance.ZeldaCameraController.GetCurrentPitch(), pitch, speed * Time.deltaTime);
        CameraRefData.Instance.ZeldaCameraController.SetRotation(newYaw, newPitch, false);
    }

    public void SetCameraScreenOffset(Vector2 target, float speed)
    {
        Vector2 current = CameraRefData.Instance.ZeldaCameraController.GetScreenOffset();
        CameraRefData.Instance.ZeldaCameraController.SetScreenOffset(Vector2.Lerp(current, target, speed * Time.deltaTime));
    }

    public void SetCameraTargetOffset(Vector3 target, float speed)
    {
        Vector3 current = CameraRefData.Instance.ZeldaCameraController.GetTargetOffset();
        CameraRefData.Instance.ZeldaCameraController.SetTargetOffset(Vector3.Lerp(current, target, speed * Time.deltaTime));
    }

    public void SetCameraScreenOffsetDirect(Vector2 offset)
    {
        CameraRefData.Instance.ZeldaCameraController?.SetScreenOffset(offset);
    }

    public void SetCameraTargetOffsetDirect(Vector3 offset)
    {
        CameraRefData.Instance.ZeldaCameraController?.SetTargetOffset(offset);
    }

    public void SetCameraZOffset(float target, float speed)
    {
        float current = CameraRefData.Instance.ZeldaCameraController.GetZOffset();
        CameraRefData.Instance.ZeldaCameraController.SetZOffset(Mathf.Lerp(current, target, speed * Time.deltaTime));
    }

    public void SetCameraZOffsetDirect(float offset)
    {
        CameraRefData.Instance.ZeldaCameraController?.SetZOffset(offset);
    }
}