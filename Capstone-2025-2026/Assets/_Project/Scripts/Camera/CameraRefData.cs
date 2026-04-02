using UnityEngine;

public class CameraRefData : MonoBehaviour
{
    public static CameraRefData Instance { get; private set; }
    public ZeldaCameraController ZeldaCameraController { get; private set; }
    public CameraModeController CameraModeController { get; private set; }
    public CameraCutsceneHandler CameraCutsceneHandler { get; private set; }
    [field: SerializeField] public CutsceneBase[] OrderedCutscenes { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        ZeldaCameraController = GetComponent<ZeldaCameraController>();
        CameraModeController = GetComponent<CameraModeController>();
        CameraCutsceneHandler = GetComponent<CameraCutsceneHandler>();
    }
}
