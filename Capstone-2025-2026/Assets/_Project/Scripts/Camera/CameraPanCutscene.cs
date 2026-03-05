using UnityEngine;

public class CameraPanCutscene : CameraCutsceneBase
{
    [SerializeField] private GameObject panTarget;
    [SerializeField] private Transform[] midPoints;
    [SerializeField] private CutsceneBase nextCutscene;

    private Transform[] _allPoints;

    public override void OnCutscenePrepare()
    {
        base.OnCutscenePrepare();
        BuildPointArray();
    }

    public override void OnCutsceneStart()
    {
        base.OnCutsceneStart();
        HandleScripts(false);
    }

    public override void OnCutsceneTick()
    {
        base.OnCutsceneTick();
        cam.transform.position = GetCatmullRomPosition(T);
        cam.transform.LookAt(panTarget.transform.position);
    }

    public override void OnCutsceneEnd()
    {
        base.OnCutsceneEnd();
        cam.transform.position = endPos.position;
        cam.transform.LookAt(panTarget.transform.position);

        if (nextCutscene != null)
            CameraCutsceneHandler.Instance.StartCutscene(nextCutscene);
    }

    private void BuildPointArray()
    {
        int midCount = midPoints != null ? midPoints.Length : 0;
        _allPoints = new Transform[midCount + 2];
        _allPoints[0] = startPos;
        for (int i = 0; i < midCount; i++)
            _allPoints[i + 1] = midPoints[i];
        _allPoints[_allPoints.Length - 1] = endPos;
    }

    private Vector3 GetCatmullRomPosition(float t)
    {
        int numSections = _allPoints.Length - 1;
        int currentSegment = Mathf.Min(Mathf.FloorToInt(t * numSections), numSections - 1);
        float localT = (t * numSections) - currentSegment;

        Vector3 p0 = _allPoints[Mathf.Max(currentSegment - 1, 0)].position;
        Vector3 p1 = _allPoints[currentSegment].position;
        Vector3 p2 = _allPoints[Mathf.Min(currentSegment + 1, _allPoints.Length - 1)].position;
        Vector3 p3 = _allPoints[Mathf.Min(currentSegment + 2, _allPoints.Length - 1)].position;

        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * localT +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * localT * localT +
            (-p0 + 3f * p1 - 3f * p2 + p3) * localT * localT * localT
        );
    }

    private void OnDrawGizmos()
    {
        if (startPos == null || endPos == null) return;

        BuildPointArray();

        if (_allPoints.Length < 2) return;

        Gizmos.color = Color.cyan;
        int resolution = 50;
        for (int i = 0; i < resolution; i++)
        {
            float t1 = (float)i / resolution;
            float t2 = (float)(i + 1) / resolution;
            Vector3 a = GetCatmullRomPosition(t1);
            Vector3 b = GetCatmullRomPosition(t2);
            Gizmos.DrawLine(a, b);
        }

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(startPos.position, 0.2f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(endPos.position, 0.2f);

        Gizmos.color = Color.yellow;
        if (midPoints != null)
            foreach (var midPoint in midPoints)
                if (midPoint != null)
                    Gizmos.DrawSphere(midPoint.position, 0.2f);
    }
}