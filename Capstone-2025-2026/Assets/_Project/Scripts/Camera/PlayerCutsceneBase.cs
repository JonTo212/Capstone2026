using UnityEngine;

public abstract class PlayerCutsceneBase : CutsceneBase
{
    [Header("Player Object")]
    [SerializeField] protected GameObject player;

    [Header("Camera Framing")]
    [SerializeField] protected Vector2 cameraScreenOffset;
    [SerializeField] protected Vector3 cameraTargetOffset;
    [SerializeField] protected float cameraZOffset = 0f;
    [SerializeField] protected float cameraOffsetBlendSpeed = 6f;

    protected Rigidbody playerRb;
    protected PlayerModelRotationHandler playerModelRotation;
    private PlayerMovement _playerMovement;

    private Vector3 _blendFrom;
    private Vector3 _blendTo;

    public override void OnCutscenePrepare()
    {
        base.OnCutscenePrepare();

        if (player == null)
            player = GameObject.FindWithTag("Player");

        if (player != null)
        {
            playerRb = player.GetComponent<Rigidbody>();
            playerModelRotation = player.GetComponent<PlayerModelRotationHandler>();
            _playerMovement = player.GetComponent<PlayerMovement>();
        }

        _blendFrom = playerRb != null ? playerRb.position : Vector3.zero;
        _blendTo = GetPlayerPosition(0f);

        DisablePlayerControl();
    }

    public override void OnBlendTick(float t)
    {
        if (playerRb != null)
            playerRb.MovePosition(Vector3.Lerp(_blendFrom, _blendTo, t));
    }

    public override void OnCutsceneUpdate()
    {
        base.OnCutsceneUpdate();

        float blendOutStart = Duration > 0f ? 1f - (BlendOutTime / Duration) : 1f;
        if (T >= blendOutStart) return;

        CameraRefData.Instance.CameraCutsceneHandler?.SetCameraScreenOffset(cameraScreenOffset, cameraOffsetBlendSpeed);
        CameraRefData.Instance.CameraCutsceneHandler?.SetCameraTargetOffset(cameraTargetOffset, cameraOffsetBlendSpeed);
        CameraRefData.Instance.CameraCutsceneHandler?.SetCameraZOffset(cameraZOffset, cameraOffsetBlendSpeed);
    }

    public override void OnCutsceneEnd()
    {
        base.OnCutsceneEnd();

        if (playerModelRotation != null)
        {
            playerModelRotation.SetSwayAngle(0f, 0f);
            playerModelRotation.SetLeanAngle(0f);
            playerModelRotation.SetNewRotationDir(null, false);
        }

        EnablePlayerControl();
    }

    public abstract Vector3 GetPlayerPosition(float t);

    private void DisablePlayerControl()
    {
        if (PlayerActions.Instance != null)
        {
            PlayerActions.Instance.DisableAllInput();
            PlayerActions.Instance.ChangeSpecificInput("DeactivateTether", true);
        }

        if (_playerMovement != null) _playerMovement.enabled = false;
        if (playerRb != null) playerRb.isKinematic = true;
    }

    private void EnablePlayerControl()
    {
        if (PlayerActions.Instance != null) PlayerActions.Instance.EnableAllInput();
        if (_playerMovement != null) _playerMovement.enabled = true;
        if (playerRb != null)
        {
            playerRb.isKinematic = false;
            playerRb.linearVelocity = Vector3.zero;
        }
    }
}