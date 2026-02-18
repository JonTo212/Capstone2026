using UnityEngine;

public class CameraCutsceneTrigger : MonoBehaviour
{
    [Tooltip("Only fire once, then disable this trigger.")]
    [SerializeField] private bool oneTimeUse = true;

    private bool _hasTriggered = false;
    private CutsceneBase _cutscene;

    private void Awake()
    {
        _cutscene = GetComponent<CutsceneBase>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (oneTimeUse && _hasTriggered) return;
        if (_cutscene == null) return;
        if (!_cutscene.IsValid()) return;
        if (CameraCutsceneHandler.Instance == null) return;

        CameraCutsceneHandler.Instance.StartCutscene(_cutscene);
        _hasTriggered = true;
    }
}