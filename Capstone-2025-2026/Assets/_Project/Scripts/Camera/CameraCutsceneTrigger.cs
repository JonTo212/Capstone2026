using FMODUnity;
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
        if (CameraRefData.Instance.CameraCutsceneHandler == null) return;

        CameraRefData.Instance.CameraCutsceneHandler.StartCutscene(_cutscene);
        _hasTriggered = true;


        RuntimeManager.PlayOneShot("event:/ZipLineGood", transform.position);
    }
}