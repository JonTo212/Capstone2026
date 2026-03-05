using UnityEngine;

public abstract class CameraCutsceneBase : CutsceneBase
{
    [SerializeField] protected Transform startPos;
    [SerializeField] protected Transform endPos;
    [SerializeField] protected Transform lookAtTarget;

    [SerializeField] private MonoBehaviour[] scriptsToModify;
    [SerializeField] private GameObject[] objectsToModify;

    [SerializeField] private GameObject[] objectsToEnableOnStart;

    protected Camera cam;

    protected virtual void Awake()
    {
        cam = Camera.main;
    }

    public override void OnCutsceneStart()
    {
        base.OnCutsceneStart();

        foreach (var objectToEnable in objectsToEnableOnStart)
            if (objectToEnable != null) objectToEnable.SetActive(true);
    }

    protected void HandleScripts(bool turnOn)
    {
        foreach (var scriptToModify in scriptsToModify)
            if (scriptToModify != null) scriptToModify.enabled = turnOn;

        foreach (var objectToModify in objectsToModify)
            if (objectToModify != null) objectToModify.SetActive(turnOn);
    }
}