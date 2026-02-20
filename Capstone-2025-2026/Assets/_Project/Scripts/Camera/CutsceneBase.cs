using System.Collections;
using UnityEngine;

public abstract class CutsceneBase : MonoBehaviour
{
    [Header("Duration")]
    [Tooltip("Total play time of the main cutscene phase in seconds.")]
    public float duration = 3f;

    [Header("Transitions")]
    [Tooltip("Seconds to pause after the trigger fires before the blend-in begins.")]
    public float blendInDelay = 0.5f;

    [Tooltip("Seconds spent blending the player / camera into the cutscene start.")]
    public float blendInTime = 0.5f;

    [Tooltip("Seconds spent blending back to normal gameplay after the cutscene ends.")]
    public float blendOutTime = 0.3f;

    [Header("Camera")]
    [Tooltip("Prevent the player from rotating the camera while the cutscene plays.")]
    public bool lockCameraInput = true;

    [Tooltip("Camera automatically rotates to face the current direction of movement.")]
    public bool autoRotateCamera = true;

    [Tooltip("Speed multiplier for auto camera rotation.")]
    public float cameraRotationSpeed = 10f;

    [Tooltip("Pitch applied to the camera during the cutscene (negative = look down).")]
    public float cameraPitch = 5f;

    [Tooltip("Apply the RopeHangCutscene CamState via CameraModeController during playback.")]
    public bool useCustomCameraState = true;

    [Header("Player")]
    [Tooltip("Disable player movement and input for the duration of the cutscene.")]
    public bool disablePlayerControl = true;

    public abstract bool IsValid();

    public abstract Vector3 GetPlayerPosition(float t);

    public virtual Vector3? GetCameraLookTarget(float t) => null;

    public virtual void OnCutscenePrepare() { }
    public virtual void OnCutsceneStart() { }

    public virtual void OnCutsceneTick(float t, Vector3 playerPos, float frameSpeed) { }

    public virtual void OnCutsceneEnd() { }
    public virtual void OnCutsceneLateUpdate() { }
}