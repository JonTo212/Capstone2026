using System.Collections.Generic;
using UnityEngine;

public class CameraCutsceneTrigger : MonoBehaviour
{
    [SerializeField] private LineRenderer ropeVisuals;

    [Header("Player Path")]
    [Tooltip("Path the player will follow while 'hanging onto the rope'")]
    [SerializeField] private List<Transform> ropePath;
    [SerializeField] private List<Transform> playerOffsetPath;

    [Tooltip("When disabled the player is placed exactly on the rope (zero offset).")]
    [SerializeField] private bool usePlayerOffset = true;

    [Header("Spline Settings")]
    [Tooltip("Use Catmull-Rom spline for smoother path interpolation")]
    [SerializeField] private bool useSpline = true;

    [Tooltip("Number of subdivisions per segment for spline path")]
    [Range(5, 50)]
    [SerializeField] private int splineSubdivisions = 20;

    [Header("Settings")]
    [Tooltip("How long the entire rope swing takes")]
    [SerializeField] private float duration = 3f;

    [Tooltip("Only trigger once, then disable")]
    [SerializeField] private bool oneTimeUse = true;

    [Header("Visualization")]
    [SerializeField] private bool showPaths = true;

    [Tooltip("Colour of the rope path gizmos.")]
    [SerializeField] private Color ropePathColor = new Color(0.2f, 0.6f, 1f, 1f);   // Blue

    [Tooltip("Colour of the player offset path gizmos.")]
    [SerializeField] private Color offsetPathColor = new Color(1f, 0.5f, 0f, 1f);   // Orange

    [Tooltip("Colour of the final combined player world-position preview.")]
    [SerializeField] private Color combinedPathColor = new Color(0.2f, 1f, 0.3f, 1f); // Green
    [SerializeField] private bool showCameraPreview = true;

    private bool _hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (oneTimeUse && _hasTriggered) return;
        if (CameraCutsceneHandler.Instance == null) return;
        if (ropePath == null || ropePath.Count < 2) return;

        List<Vector3> combinedPath = BuildCombinedPath();

        // Register offset sampler so CameraCutsceneHandler can expose the rope attachment position
        // (rope position = combined player position - offset at that t)
        CameraCutsceneHandler.Instance.SetOffsetSampler(SampleOffsetPath);
        CameraCutsceneHandler.Instance.StartRopeSwingWithSpline(combinedPath, duration);
        _hasTriggered = true;
    }

    private void LateUpdate()
    {
        DrawRope();
    }

    private void DrawRope()
    {
        List<Vector3> positions = new List<Vector3>();
        foreach (var position in ropePath)
        {
            positions.Add(position.position);
        }

        Vector3[] smoothedPoints = LineSmoother.SmoothLine(positions.ToArray(), 0.1f);

        ropeVisuals.positionCount = smoothedPoints.Length;
        ropeVisuals.SetPositions(smoothedPoints);
        ropeVisuals.startWidth = 0.1f;
        ropeVisuals.endWidth = 0.1f;
    }

    private List<Vector3> BuildCombinedPath()
    {
        int totalSegments = ropePath.Count - 1;
        int totalPoints = totalSegments * splineSubdivisions + 1;

        List<Vector3> combined = new List<Vector3>(totalPoints);

        for (int i = 0; i < totalSegments; i++)
        {
            for (int j = 0; j < splineSubdivisions; j++)
            {
                float localT = j / (float)splineSubdivisions;
                float globalT = (i + localT) / totalSegments;

                Vector3 ropePoint = SampleRopePath(i, localT);
                Vector3 offset = SampleOffsetPath(globalT);
                combined.Add(ropePoint + offset);
            }
        }

        Vector3 lastRope = ropePath[ropePath.Count - 1].position;
        Vector3 lastOffset = SampleOffsetPath(1f);
        combined.Add(lastRope + lastOffset);

        return combined;
    }

    private Vector3 SampleRopePath(int segmentIndex, float localT)
    {
        if (!useSpline || ropePath.Count < 3)
        {
            // Linear fallback
            return Vector3.Lerp(
                ropePath[segmentIndex].position,
                ropePath[segmentIndex + 1].position,
                localT);
        }

        Vector3 p0 = GetRopePoint(segmentIndex - 1);
        Vector3 p1 = GetRopePoint(segmentIndex);
        Vector3 p2 = GetRopePoint(segmentIndex + 1);
        Vector3 p3 = GetRopePoint(segmentIndex + 2);

        return CatmullRom(p0, p1, p2, p3, localT);
    }

    private Vector3 GetRopePoint(int index)
    {
        if (index < 0)
        {
            // Extrapolate before start
            Vector3 p1 = ropePath[0].position;
            Vector3 p2 = ropePath[1].position;
            return p1 + (p1 - p2);
        }
        if (index >= ropePath.Count)
        {
            // Extrapolate past end
            int last = ropePath.Count - 1;
            Vector3 pA = ropePath[last].position;
            Vector3 pB = ropePath[last - 1].position;
            return pA + (pA - pB);
        }
        return ropePath[index].position;
    }

    private Vector3 SampleOffsetPath(float globalT)
    {
        if (!usePlayerOffset || playerOffsetPath == null || playerOffsetPath.Count == 0)
            return Vector3.zero;

        if (playerOffsetPath.Count == 1)
            return playerOffsetPath[0].position;

        int segments = playerOffsetPath.Count - 1;
        float scaled = globalT * segments;
        int segIndex = Mathf.FloorToInt(scaled);
        segIndex = Mathf.Clamp(segIndex, 0, segments - 1);
        float localT = scaled - segIndex;

        if (!useSpline || playerOffsetPath.Count < 3)
        {
            return Vector3.Lerp(
                playerOffsetPath[segIndex].position,
                playerOffsetPath[segIndex + 1].position,
                localT);
        }

        Vector3 p0 = GetOffsetPoint(segIndex - 1);
        Vector3 p1 = GetOffsetPoint(segIndex);
        Vector3 p2 = GetOffsetPoint(segIndex + 1);
        Vector3 p3 = GetOffsetPoint(segIndex + 2);

        return CatmullRom(p0, p1, p2, p3, localT);
    }

    private Vector3 GetOffsetPoint(int index)
    {
        if (index < 0)
        {
            Vector3 p1 = playerOffsetPath[0].localPosition;
            Vector3 p2 = playerOffsetPath[1].localPosition;
            return p1 + (p1 - p2);
        }
        if (index >= playerOffsetPath.Count)
        {
            int last = playerOffsetPath.Count - 1;
            Vector3 pA = playerOffsetPath[last].localPosition;
            Vector3 pB = playerOffsetPath[last - 1].localPosition;
            return pA + (pA - pB);
        }
        return playerOffsetPath[index].localPosition;
    }

    //honestly no clue how this works but it's a spline function
    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return p1
             + 0.5f * (p2 - p0) * t
             + 0.5f * (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
             + 0.5f * (-p0 + 3f * p1 - 3f * p2 + p3) * t3;
    }

    #region Gizmos
    private void OnDrawGizmos()
    {
        if (!showPaths) return;

        DrawPathGizmo(ropePath, ropePathColor);
        DrawCombinedPreview();
    }

    private void DrawPathGizmo(List<Transform> path, Color color)
    {
        if (path == null || path.Count < 2) return;

        // Waypoint spheres
        for (int i = 0; i < path.Count; i++)
        {
            if (path[i] == null) continue;

            Gizmos.color = (i == 0) ? Color.green :
                           (i == path.Count - 1) ? Color.red :
                           color;

            Gizmos.DrawSphere(path[i].position, 0.25f);

            if (i < path.Count - 1 && path[i + 1] != null)
            {
                Vector3 dir = (path[i + 1].position - path[i].position).normalized;
                DrawArrow(path[i].position, dir, 0.5f, color);
            }
        }

        // Curve
        Gizmos.color = color;
        bool canSpline = useSpline && path.Count >= 3;

        if (canSpline)
        {
            int segs = path.Count - 1;
            for (int i = 0; i < segs; i++)
            {
                Vector3 prev = path[i].position;
                for (int j = 1; j <= 20; j++)
                {
                    float lt = j / 20f;
                    Vector3 next;
                    if (path == ropePath)
                        next = SampleRopePath(i, lt);
                    else
                        next = CatmullRom(
                            GetOffsetPoint(i - 1),
                            GetOffsetPoint(i),
                            GetOffsetPoint(i + 1),
                            GetOffsetPoint(i + 2),
                            lt);
                    Gizmos.DrawLine(prev, next);
                    prev = next;
                }
            }
        }
        else
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                if (path[i] != null && path[i + 1] != null)
                    Gizmos.DrawLine(path[i].position, path[i + 1].position);
            }
        }
    }

    private void DrawCombinedPreview()
    {
        if (ropePath == null || ropePath.Count < 2) return;

        Gizmos.color = combinedPathColor;

        int totalSegments = ropePath.Count - 1;
        Vector3? prev = null;

        // 1. Draw spheres at offset control points (NOT rope points)
        if (usePlayerOffset && playerOffsetPath != null && playerOffsetPath.Count > 0)
        {
            for (int i = 0; i < playerOffsetPath.Count; i++)
            {
                if (playerOffsetPath[i] == null) continue;

                float globalT = i / (float)(playerOffsetPath.Count - 1);
                Vector3 ropePoint = SampleRopePath(
                    Mathf.Clamp(Mathf.FloorToInt(globalT * totalSegments), 0, totalSegments - 1),
                    (globalT * totalSegments) % 1f
                );

                Vector3 offset = playerOffsetPath[i].localPosition;
                Vector3 combined = ropePoint + offset;

                Gizmos.DrawSphere(combined, 0.18f);
            }
        }

        // 2. Draw the actual combined spline curve (line only)
        for (int seg = 0; seg < totalSegments; seg++)
        {
            for (int j = 0; j < splineSubdivisions; j++)
            {
                float localT = j / (float)splineSubdivisions;
                float globalT = (seg + localT) / totalSegments;

                Vector3 ropePoint = SampleRopePath(seg, localT);
                Vector3 offset = SampleOffsetPath(globalT);
                Vector3 combined = ropePoint + offset;

                if (prev.HasValue)
                    Gizmos.DrawLine(prev.Value, combined);

                prev = combined;
            }
        }

        // Final endpoint
        Vector3 last = ropePath[ropePath.Count - 1].position + SampleOffsetPath(1f);
        if (prev.HasValue)
            Gizmos.DrawLine(prev.Value, last);
    }



    private static void DrawArrow(Vector3 origin, Vector3 dir, float length, Color color)
    {
        Vector3 tip = origin + dir * length;
        Gizmos.color = color;
        Gizmos.DrawLine(origin, tip);

        Vector3 right = Quaternion.Euler(0f, 20f, 0f) * -dir;
        Vector3 left = Quaternion.Euler(0f, -20f, 0f) * -dir;

        Gizmos.DrawLine(tip, tip + right * (length * 0.3f));
        Gizmos.DrawLine(tip, tip + left * (length * 0.3f));
    }
    #endregion
}