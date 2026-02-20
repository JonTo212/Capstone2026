using System.Collections;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class CameraPanCutscene : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private GameObject playerObj;
    [SerializeField] private GameObject panTarget;
    [SerializeField] private MonoBehaviour[] sciptsToDisable;
    [SerializeField] private GameObject[] objectsToDisable;
    [SerializeField] private ZoomToPlayerCutscene nextCutscene;

    private void Start()
    {
        if (_camera == null) _camera = Camera.main;
        foreach (var script in sciptsToDisable)
        {
            script.enabled = false;
        }
        foreach (var obj in objectsToDisable)
        {
            obj.SetActive(false);
        }
        nextCutscene.enabled = false;
        StartCoroutine(PanCutscene());
    }

    private void OnCutsceneEnd()
    {
        nextCutscene.enabled = true;
        this.enabled = false;
    }

    [SerializeField] private Transform[] splinePoints;
    [SerializeField] private float duration = 5f;

    private IEnumerator PanCutscene()
    {
        if (panTarget == null || splinePoints.Length < 2) yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration; // 0 to 1 along the whole spline

            _camera.transform.position = GetCatmullRomPosition(t, splinePoints);
            _camera.transform.LookAt(panTarget.transform.position);

            yield return null;
        }

        // Snap to final point
        _camera.transform.position = splinePoints[splinePoints.Length - 1].position;
        _camera.transform.LookAt(panTarget.transform.position);

        OnCutsceneEnd();
    }

    private Vector3 GetCatmullRomPosition(float t, Transform[] points)
    {
        // Figure out which segment we're in
        int numSections = points.Length - 1;
        int currentSegment = Mathf.Min(Mathf.FloorToInt(t * numSections), numSections - 1);
        float localT = (t * numSections) - currentSegment;

        // Clamp control points so ends don't need extras
        Vector3 p0 = points[Mathf.Max(currentSegment - 1, 0)].position;
        Vector3 p1 = points[currentSegment].position;
        Vector3 p2 = points[Mathf.Min(currentSegment + 1, points.Length - 1)].position;
        Vector3 p3 = points[Mathf.Min(currentSegment + 2, points.Length - 1)].position;

        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * localT +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * localT * localT +
            (-p0 + 3f * p1 - 3f * p2 + p3) * localT * localT * localT
        );
    }

    private void OnDrawGizmos()
    {
        if (splinePoints == null || splinePoints.Length < 2) return;

        Gizmos.color = Color.cyan;
        int resolution = 50;
        for (int i = 0; i < resolution; i++)
        {
            float t1 = (float)i / resolution;
            float t2 = (float)(i + 1) / resolution;
            Vector3 a = GetCatmullRomPosition(t1, splinePoints);
            Vector3 b = GetCatmullRomPosition(t2, splinePoints);
            Gizmos.DrawLine(a, b);
        }

        // Draw spheres at each control point
        Gizmos.color = Color.yellow;
        foreach (var point in splinePoints)
        {
            if (point != null)
                Gizmos.DrawSphere(point.position, 0.2f);
        }
    }
}
