using UnityEngine;

public class ZoomToPlayerCutscene : CameraCutsceneBase
{
    [SerializeField] private float startFOV;
    [SerializeField] private float endFOV;

    public override void OnCutsceneStart()
    {
        base.OnCutsceneStart();
        cam.fieldOfView = startFOV;
        cam.transform.position = startPos.position;
    }

    public override void OnCutsceneTick()
    {
        base.OnCutsceneTick();
        cam.fieldOfView = Mathf.Lerp(startFOV, endFOV, T);
        cam.transform.position = Vector3.Lerp(startPos.position, endPos.position, T);
        cam.transform.LookAt(lookAtTarget.position);
    }

    public override void OnCutsceneEnd()
    {
        base.OnCutsceneEnd();
        cam.fieldOfView = endFOV;
        cam.transform.position = endPos.position;

        HandleScripts(true);

        ZeldaCameraController zeldaCam = cam.GetComponent<ZeldaCameraController>();
        if (zeldaCam != null)
        {
            Vector3 euler = cam.transform.eulerAngles;
            float pitch = euler.x > 180f ? euler.x - 360f : euler.x;
            zeldaCam.SetRotation(euler.y, pitch);
        }
    }    
}