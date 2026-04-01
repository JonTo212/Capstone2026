using UnityEngine;
using System.Collections;

public class CrashLandCutscene : PlayerCutsceneBase
{
    [Header("Falling Path")]
    [SerializeField] private Transform startPos;
    [SerializeField] private Transform endPos;
    [Tooltip("Controls the speed and easing of the player falling.")]
    [SerializeField] private AnimationCurve fallCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Skydiving Camera Approach")]
    [Tooltip("Controls the speed and easing of the camera crashing down.")]
    [SerializeField] private AnimationCurve cameraApproachCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Screen Space Framing")]
    [Tooltip("Start and end screen offsets (X = Horizontal, Y = Vertical).")]
    [SerializeField] private Vector2 startScreenOffset = new Vector2(0f, 0.5f);
    [SerializeField] private Vector2 endScreenOffset = new Vector2(0f, 0f);

    [Header("Distance and Pitch")]
    [Tooltip("Start zoomed out, end at 0 for a first-person impact.")]
    [SerializeField] private float startCameraDistance = 15f;
    [SerializeField] private float endCameraDistance = 0f;

    [Tooltip("Start looking down from above, flatten out on impact.")]
    [SerializeField] private float cameraStartPitch = 60f;
    [SerializeField] private float cameraEndPitch = 10f;
    [SerializeField] private float cameraRotationSpeed = 10f;

    [Header("Transitions / Animations")]
    [SerializeField] private ImageFader fadeToBlackScript; // Replace with your actual class name
    [SerializeField] private Animator anim;

    public override void OnCutscenePrepare()
    {
        Vector3 fwd = GetFallDirection();
        Vector3 endFwdFlat = new Vector3(fwd.x, 0f, fwd.z);

        CameraRefData.Instance.CameraCutsceneHandler?.SetCameraPositionDamping(Vector3.zero);
        CameraRefData.Instance.CameraCutsceneHandler.SetRotationDirect(GetHorizontalDirection(), cameraStartPitch);
        PlayerRefData.Instance.PlayerFade.SetFade(false);
        anim.SetBool("FallCutscene", true);
        HandleScripts(false);

        // Handle the fade-in and pause the cutscene logic until complete
        StartCoroutine(HandleFadeBeforeCutscene());

        base.OnCutscenePrepare();
    }

    private IEnumerator HandleFadeBeforeCutscene()
    {
        if (fadeToBlackScript == null) yield break;

        SetBlendDelayActive(true);

        fadeToBlackScript.SetImageAlpha(1f);
        fadeToBlackScript.gameObject.SetActive(true);
        fadeToBlackScript.FadeOut();

        yield return new WaitUntil(() => fadeToBlackScript.FadeComplete);

        SetBlendDelayActive(false);
    }

    public override void OnCutsceneUpdate()
    {
        // 1. Calculate what the screen offset SHOULD be at this point in the curve
        float camProgress = cameraApproachCurve.Evaluate(T);
        cameraScreenOffset = Vector2.Lerp(startScreenOffset, endScreenOffset, camProgress);

        // 2. Call the base method so it actually applies this offset to the Handler
        base.OnCutsceneUpdate();
    }

    public override void OnCutsceneTick()
    {
        base.OnCutsceneTick();

        // 1. Move Player
        if (playerRb != null) playerRb.MovePosition(GetPlayerPosition(T));

        // 2. Rotate Player
        Vector3 forward = GetFallDirection();
        if (forward.sqrMagnitude > 0.01f)
        {
            playerModelRotation?.SetNewRotationDir(Quaternion.LookRotation(forward), true);
        }

        // 3. Update Camera Dive
        ApplySkydivingCameraState(T);
    }

    private void ApplySkydivingCameraState(float t)
    {
        float camProgress = cameraApproachCurve.Evaluate(t);

        // LERP SCREEN OFFSET (Instead of World Target Offset)
        Vector2 currentScreenOffset = Vector2.Lerp(startScreenOffset, endScreenOffset, camProgress);
        CameraRefData.Instance.CameraCutsceneHandler?.SetCameraScreenOffsetDirect(currentScreenOffset);

        // Distance & Pitch logic
        float currentDistance = Mathf.Lerp(startCameraDistance, endCameraDistance, camProgress);
        float currentPitch = Mathf.Lerp(cameraStartPitch, cameraEndPitch, camProgress);

        float defaultDist = CameraRefData.Instance.ZeldaCameraController != null ? CameraRefData.Instance.ZeldaCameraController.GetDefaultDistance() : 5f;
        float requiredZOffset = currentDistance - defaultDist;

        CameraRefData.Instance.CameraCutsceneHandler?.SetCameraZOffsetDirect(requiredZOffset);

        Vector3 dir = GetHorizontalDirection();
        if (dir.sqrMagnitude > 0.01f)
        {
            CameraRefData.Instance.CameraCutsceneHandler?.RotateCameraToDirection(dir, cameraRotationSpeed, currentPitch);
        }
    }

    public override void OnCutsceneEnd()
    {
        base.OnCutsceneEnd();
        CameraRefData.Instance.CameraCutsceneHandler?.SetCameraPositionDamping(null);
        CleanUpCamera();
        fadeToBlackScript.SetImageAlpha(1f);
        PlayerRefData.Instance.PlayerFade.SetFade(true);
        if (playerRb != null) playerRb.position = GetPlayerPosition(1f);
        playerModelRotation?.SetNewRotationDir(Quaternion.LookRotation(GetFallDirection()), true);
        HandleScripts(true);
    }

    public override void OnCutsceneSkip()
    {
        base.OnCutsceneSkip();
        CameraRefData.Instance.CameraCutsceneHandler?.SetCameraPositionDamping(null);
        CleanUpCamera();
        fadeToBlackScript.SetImageAlpha(1f);
        PlayerRefData.Instance.PlayerFade.SetFade(true);
        if (playerRb != null) playerRb.position = GetPlayerPosition(1f);
        playerModelRotation?.SetNewRotationDir(Quaternion.LookRotation(GetFallDirection()), true);
        HandleScripts(true);
    }

    private void CleanUpCamera()
    {
        CameraRefData.Instance.CameraCutsceneHandler?.SetCameraZOffsetDirect(0f);
        CameraRefData.Instance.CameraCutsceneHandler?.SetCameraScreenOffsetDirect(Vector2.zero);
    }

    public override Vector3 GetPlayerPosition(float t)
    {
        if (startPos == null || endPos == null) return Vector3.zero;
        return Vector3.Lerp(startPos.position, endPos.position, fallCurve.Evaluate(t));
    }

    private Vector3 GetFallDirection() => (endPos != null && startPos != null) ? (endPos.position - startPos.position).normalized : Vector3.forward;

    private Vector3 GetHorizontalDirection()
    {
        Vector3 dir = GetFallDirection();
        return new Vector3(dir.x, 0f, dir.z).normalized;
    }
}