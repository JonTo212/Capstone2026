using UnityEngine;

public abstract class CameraCutsceneBase : CutsceneBase
{
    [SerializeField] protected Transform startPos;
    [SerializeField] protected Transform endPos;
    [SerializeField] protected Transform lookAtTarget;

    protected Camera cam;

    protected virtual void Awake()
    {
        cam = Camera.main;
    }

    public override void OnCutsceneStart()
    {
        base.OnCutsceneStart();

        PlayerRefData.Instance.PlayerMovement.SetGrabbing(true);

        foreach (var objectToEnable in objectsToEnableOnStart)
            if (objectToEnable != null) objectToEnable.SetActive(true);
    }

    public override void OnCutsceneEnd()
    {
        base.OnCutsceneEnd();

        PlayerRefData.Instance.PlayerMovement.SetGrabbing(false);
    }
}