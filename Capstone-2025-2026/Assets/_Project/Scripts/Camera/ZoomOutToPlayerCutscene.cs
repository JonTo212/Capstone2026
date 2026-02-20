using System.Collections;
using UnityEngine;

public class ZoomToPlayerCutscene : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private Transform lookAtTarget;
    [SerializeField] private GameObject playerObj;
    [SerializeField] private float startFOV;
    [SerializeField] private float endFOV;
    [SerializeField] private Transform startPos;
    [SerializeField] private Transform endPos;
    [SerializeField] private MonoBehaviour[] sciptsToEnable;

    private void Awake()
    {
        if (_camera == null) _camera = Camera.main;
        if(playerObj == null) playerObj = GameObject.FindWithTag("Player");
    }

    private void Start()
    {
        playerObj.SetActive(true);
        StartCoroutine(ZoomOutToPlayerCutscene());
    }

    private void OnCutsceneEnd()
    {
        foreach (var script in sciptsToEnable)
        {
            script.enabled = true;
        }
    }

    [SerializeField] private float duration = 5f;

    private IEnumerator ZoomOutToPlayerCutscene()
    {
        float elapsed = 0;
        _camera.fieldOfView = startFOV;
        _camera.transform.position = startPos.position;

        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            _camera.fieldOfView = Mathf.Lerp(startFOV, endFOV, t);
            _camera.transform.position = Vector3.Lerp(startPos.position, endPos.position, t);
            _camera.transform.LookAt(lookAtTarget.position);
            elapsed += Time.deltaTime;

            yield return null;
        }

        OnCutsceneEnd();
        _camera.fieldOfView = endFOV;
        _camera.transform.position = endPos.position;
    }
}
