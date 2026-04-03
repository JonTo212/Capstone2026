using UnityEngine;

public class NPCKeyCutscene : CameraCutsceneBase
{
    private float _holdFraction = 0.4f; //% of duration that is spent frozen and looking at target
    [SerializeField] private float lookAtFOV = 0f;

    private float _savedFOV;
    private float _targetFOV;
    private Vector3 _returnTargetPos;
    private Quaternion _returnTargetRot;

    private Vector3 _blendFromPos;
    private Quaternion _blendFromRot;

    public void Configure(Transform start, Transform lookAt, float duration, float holdFraction, float blendInDelay, float blendInTime)
    {
        startPos = start;
        lookAtTarget = lookAt;
        _holdFraction = holdFraction;
        Duration = duration;
        BlendInDelay = blendInDelay;
        BlendInTime = blendInTime;
    }

    public override void OnCutscenePrepare()
    {
        base.OnCutscenePrepare();

        _savedFOV = cam.fieldOfView;
        _targetFOV = (lookAtFOV > 0f) ? lookAtFOV : _savedFOV;

        //snapshot where the camera is right now, before anything moves
        _blendFromPos = cam.transform.position;
        _blendFromRot = cam.transform.rotation;

        CameraRefData.Instance.ZeldaCameraController.SetFrozen(true);

        PlayerRefData.Instance.PlayerMovement.SetGrabbing(true);
        PlayerRefData.Instance.LassoTetherController.SetLassoState(false);
        PlayerRefData.Instance.LassoTetherController.SetTetherState(false);

        HandleScripts(false);
    }

    public override void OnBlendTick(float t)
    {
        cam.transform.position = Vector3.Lerp(_blendFromPos, startPos.position, t);
        cam.transform.rotation = Quaternion.Slerp(_blendFromRot, Quaternion.LookRotation(lookAtTarget.position - startPos.position), t);
    }

    public override void OnCutsceneStart()
    {
        base.OnCutsceneStart();

        CameraRefData.Instance.ZeldaCameraController.SetFrozen(true);
        CameraRefData.Instance.CameraModeController.ForceSnapToCurrentState();
        CameraRefData.Instance.ZeldaCameraController.SnapSmoothedPosition();
        CameraRefData.Instance.ZeldaCameraController.UpdateGhostTransformPublic();

        _returnTargetPos = CameraRefData.Instance.ZeldaCameraController.GetGhostPosition();
        _returnTargetRot = CameraRefData.Instance.ZeldaCameraController.GetGhostRotation();

        //no snap here - blend already moved us to startPos
        cam.fieldOfView = _targetFOV;
        cam.transform.LookAt(lookAtTarget.position);
    }

    public override void OnCutsceneTick()
    {
        base.OnCutsceneTick();

        //stay frozen and look at the target for the first X% of Duration
        if (T < _holdFraction)
        {
            cam.transform.position = startPos.position;
            cam.transform.LookAt(lookAtTarget.position);
            cam.fieldOfView = _targetFOV;
        }

        //start moving back
        else
        {
            float returnT = Mathf.Clamp01((T - _holdFraction) / (1f - _holdFraction));
            float smooth = Mathf.SmoothStep(0f, 1f, returnT);

            cam.transform.position = Vector3.Lerp(startPos.position, _returnTargetPos, smooth);
            cam.transform.rotation = Quaternion.Slerp(Quaternion.LookRotation(lookAtTarget.position - startPos.position), _returnTargetRot, smooth);
            cam.fieldOfView = Mathf.Lerp(_targetFOV, _savedFOV, smooth);
        }
    }

    public override void OnCutsceneEnd()
    {
        cam.fieldOfView = _savedFOV;

        HandleScripts(true);

        CameraRefData.Instance.ZeldaCameraController.SetFrozen(false);
        PlayerRefData.Instance.PlayerMovement.SetGrabbing(false);
        PlayerRefData.Instance.LassoTetherController.SetLassoState(true);
        PlayerRefData.Instance.LassoTetherController.SetTetherState(true);


        base.OnCutsceneEnd();
    }

    public override void OnCutsceneSkip()
    {
        cam.fieldOfView = _savedFOV;

        CameraRefData.Instance.ZeldaCameraController.SetFrozen(false);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (startPos != null && lookAtTarget != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(startPos.position, 0.2f);
            Gizmos.DrawLine(startPos.position, lookAtTarget.position);

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(lookAtTarget.position, 0.2f);
        }
    }
#endif
}