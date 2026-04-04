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
    [SerializeField] private ImageFader fadeToBlackScript;
    [SerializeField] private float fadeInDuration = 1.8f;
    [SerializeField] private Animator anim;

    [Header("Fall Wobble")]
    [Tooltip("How strongly the camera wobbles on the screen offset axes during the fall.")]
    [SerializeField] private float wobbleScreenStrength = 0.04f;
    [Tooltip("How strongly the camera wobbles on the Z (distance) axis during the fall.")]
    [SerializeField] private float wobbleZStrength = 0.3f;
    [Tooltip("Frequency of the Perlin noise wobble. Higher = more chaotic.")]
    [SerializeField] private float wobbleFrequency = 1.8f;

    private float _wobbleSeedX;
    private float _wobbleSeedY;
    private float _wobbleSeedZ;
    private float _perlinOffsetY;
    private float _perlinOffsetZ;


    public override void OnCutscenePrepare()
    {
        //randomize perlin noise values so the wobble isn't identical each time
        _wobbleSeedX = Random.Range(0f, 100f);
        _wobbleSeedY = Random.Range(0f, 100f);
        _wobbleSeedZ = Random.Range(0f, 100f);
        _perlinOffsetY = Random.Range(25f, 50f);
        _perlinOffsetZ = Random.Range(75f, 100f);

        Vector3 fwd = GetFallDirection();
        Vector3 endFwdFlat = new Vector3(fwd.x, 0f, fwd.z);

        CameraRefData.Instance.CameraCutsceneHandler?.SetCameraPositionDamping(Vector3.zero);
        CameraRefData.Instance.CameraCutsceneHandler.SetRotationDirect(GetHorizontalDirection(), cameraStartPitch);
        PlayerRefData.Instance.PlayerFade.SetFade(false);

        anim.SetBool("FallCutscene", true);
        HandleScripts(false);

        StartCoroutine(HandleFadeBeforeCutscene());

        base.OnCutscenePrepare();
    }

    private IEnumerator HandleFadeBeforeCutscene()
    {
        if (fadeToBlackScript == null) yield break;

        SetBlendDelayActive(true);

        fadeToBlackScript.gameObject.SetActive(true);
        fadeToBlackScript.SetImageAlpha(1f);
        fadeToBlackScript.duration = fadeInDuration;
        fadeToBlackScript.FadeOut();

        yield return new WaitUntil(() => fadeToBlackScript.FadeComplete);

        SetBlendDelayActive(false);
    }

    public override void OnCutsceneUpdate()
    {
        base.OnCutsceneUpdate();

        float camProgress = cameraApproachCurve.Evaluate(T);
        cameraScreenOffset = Vector2.Lerp(startScreenOffset, endScreenOffset, camProgress);

        ApplySkydivingCameraState(T);
    }

    public override void OnCutsceneTick()
    {
        base.OnCutsceneTick();

        if (playerRb != null) playerRb.MovePosition(GetPlayerPosition(T));

        Vector3 forward = GetFallDirection();
        if (forward.sqrMagnitude > 0.01f)
            playerModelRotation?.SetNewRotationDir(Quaternion.LookRotation(forward), true);
    }

    private void ApplySkydivingCameraState(float t)
    {
        float camProgress = cameraApproachCurve.Evaluate(t);

        //base screen offset from the approach curve
        Vector2 baseScreenOffset = Vector2.Lerp(startScreenOffset, endScreenOffset, camProgress);

        //perlin noise -> between -1 and 1 so it can move in all directions
        float noiseTime = Time.time * wobbleFrequency;
        float nx = (Mathf.PerlinNoise(_wobbleSeedX, noiseTime) - 0.5f) * 2f;
        float ny = (Mathf.PerlinNoise(_wobbleSeedY, noiseTime) - 0.5f) * 2f;
        float nz = (Mathf.PerlinNoise(_wobbleSeedZ, noiseTime) - 0.5f) * 2f;

        //fade out more aggressively in the last 20% of the fall
        float wobbleIntensity = Mathf.Clamp01((1f - t) / 0.2f);

        Vector2 wobbleScreen = new Vector2(nx, ny) * wobbleScreenStrength * wobbleIntensity;
        float wobbleZ = nz * wobbleZStrength * wobbleIntensity;

        CameraRefData.Instance.CameraCutsceneHandler?.SetCameraScreenOffsetDirect(baseScreenOffset + wobbleScreen);

        //distance + pitch
        float currentDistance = Mathf.Lerp(startCameraDistance, endCameraDistance, camProgress);
        float currentPitch = Mathf.Lerp(cameraStartPitch, cameraEndPitch, camProgress);

        float defaultDist = CameraRefData.Instance.ZeldaCameraController != null ? CameraRefData.Instance.ZeldaCameraController.GetDefaultDistance() : 5f;
        float requiredZOffset = (currentDistance - defaultDist) + wobbleZ;

        CameraRefData.Instance.CameraCutsceneHandler?.SetCameraZOffsetDirect(requiredZOffset);

        Vector3 dir = GetHorizontalDirection();
        if (dir.sqrMagnitude > 0.01f)
            CameraRefData.Instance.CameraCutsceneHandler?.RotateCameraToDirection(dir, cameraRotationSpeed, currentPitch);
    }

    public override void OnCutsceneEnd()
    {
        base.OnCutsceneEnd();
        HandleCutsceneFinish();
    }

    public override void OnCutsceneSkip()
    {
        base.OnCutsceneSkip();
        HandleCutsceneFinish();
    }

    private void HandleCutsceneFinish()
    {
        CameraRefData.Instance.CameraCutsceneHandler?.SetCameraPositionDamping(null);
        CleanUpCamera();

        fadeToBlackScript.SetImageAlpha(1f);
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

    private Vector3 GetFallDirection() =>
        (endPos != null && startPos != null) ? (endPos.position - startPos.position).normalized : Vector3.forward;

    private Vector3 GetHorizontalDirection()
    {
        Vector3 dir = GetFallDirection();
        return new Vector3(dir.x, 0f, dir.z).normalized;
    }
}