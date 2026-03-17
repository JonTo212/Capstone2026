using FMODUnity;
using UnityEngine;

public class RopeCutsceneActivator : Prop
{
    [Tooltip("Only fire once, then disable this trigger.")]
    [SerializeField] private bool oneTimeUse = true;

    private bool _hasTriggered = false;
    private CutsceneBase _cutscene;

    private void Awake()
    {
        Init();
        _cutscene = GetComponent<CutsceneBase>();
        OnPropSnared += RunCutscene;
    }

    private void RunCutscene()
    {
        if (oneTimeUse && _hasTriggered) return;
        if (_cutscene == null) return;
        if (CameraCutsceneHandler.Instance == null) return;

        CameraCutsceneHandler.Instance.StartCutscene(_cutscene);
        _hasTriggered = true;

        RuntimeManager.PlayOneShot("event:/ZipLineGood", transform.position);
    }

}
