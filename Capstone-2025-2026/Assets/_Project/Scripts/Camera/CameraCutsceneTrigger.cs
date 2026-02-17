using System.Collections.Generic;
using UnityEngine;

public class CameraCutsceneTrigger : MonoBehaviour
{
    [Header("Player Path")]
    [Tooltip("Path the player will follow while 'hanging onto the rope'")]
    [SerializeField] private List<Transform> playerPath;

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
    [SerializeField] private bool showPath = true;
    [SerializeField] private Color pathColor = new Color(1f, 0.5f, 0f, 1f); // Orange
    [SerializeField] private bool showCameraPreview = true;
    [SerializeField] private int previewPoints = 10;

    private bool _hasTriggered = false;
    private List<Vector3> _cachedSplinePath;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (oneTimeUse && _hasTriggered) return;

        if (CameraCutsceneHandler.Instance != null &&
            playerPath != null && playerPath.Count >= 2)
        {
            // Generate spline path if enabled
            if (useSpline && playerPath.Count >= 3)
            {
                _cachedSplinePath = GenerateSplinePath();
                CameraCutsceneHandler.Instance.StartRopeSwingWithSpline(_cachedSplinePath, duration);
            }
            else
            {
                CameraCutsceneHandler.Instance.StartRopeSwing(playerPath, duration);
            }

            _hasTriggered = true;
        }
    }

    private List<Vector3> GenerateSplinePath()
    {
        List<Vector3> splinePath = new List<Vector3>();

        int totalSegments = playerPath.Count - 1;

        for (int i = 0; i < totalSegments; i++)
        {
            for (int j = 0; j < splineSubdivisions; j++)
            {
                float t = j / (float)splineSubdivisions;
                Vector3 point = GetSplinePoint(i, t);
                splinePath.Add(point);
            }
        }

        // Add final point
        splinePath.Add(playerPath[playerPath.Count - 1].position);

        return splinePath;
    }

    private Vector3 GetSplinePoint(int segmentIndex, float t)
    {
        // Get the four control points for Catmull-Rom spline
        Vector3 p0, p1, p2, p3;

        p1 = playerPath[segmentIndex].position;
        p2 = playerPath[segmentIndex + 1].position;

        // Handle start and end cases
        if (segmentIndex == 0)
            p0 = p1 + (p1 - p2); // Extrapolate backwards
        else
            p0 = playerPath[segmentIndex - 1].position;

        if (segmentIndex >= playerPath.Count - 2)
            p3 = p2 + (p2 - p1); // Extrapolate forwards
        else
            p3 = playerPath[segmentIndex + 2].position;

        return CatmullRomSpline(p0, p1, p2, p3, t);
    }

    //honestly no clue how this works but it's a spline function
    private Vector3 CatmullRomSpline(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        Vector3 result =
            p1 +
            0.5f * (p2 - p0) * t +
            0.5f * (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            0.5f * (-p0 + 3f * p1 - 3f * p2 + p3) * t3;

        return result;
    }

    private void OnDrawGizmos()
    {
        if (!showPath) return;
        if (playerPath == null || playerPath.Count < 2) return;

        // Draw waypoint spheres first
        for (int i = 0; i < playerPath.Count; i++)
        {
            if (playerPath[i] != null)
            {
                if (i == 0)
                    Gizmos.color = Color.green; // Start point
                else if (i == playerPath.Count - 1)
                    Gizmos.color = Color.red; // End point
                else
                    Gizmos.color = pathColor;

                Gizmos.DrawSphere(playerPath[i].position, 0.3f);

                // Draw arrow showing direction at each waypoint
                if (i < playerPath.Count - 1)
                {
                    Vector3 direction = (playerPath[i + 1].position - playerPath[i].position).normalized;
                    DrawArrow(playerPath[i].position, direction, 0.5f);
                }
            }
        }

        // Draw the actual path (spline or linear)
        Gizmos.color = pathColor;

        if (useSpline && playerPath.Count >= 3)
        {
            // Draw smooth spline path
            DrawSplinePath();
        }
        else
        {
            // Draw linear path
            for (int i = 0; i < playerPath.Count - 1; i++)
            {
                if (playerPath[i] != null && playerPath[i + 1] != null)
                {
                    Gizmos.DrawLine(playerPath[i].position, playerPath[i + 1].position);
                }
            }
        }
    }

    private void DrawSplinePath()
    {
        int totalSegments = playerPath.Count - 1;
        int pointsPerSegment = 20; // Higher = smoother visualization

        for (int i = 0; i < totalSegments; i++)
        {
            Vector3 previousPoint = GetSplinePoint(i, 0f);

            for (int j = 1; j <= pointsPerSegment; j++)
            {
                float t = j / (float)pointsPerSegment;
                Vector3 currentPoint = GetSplinePoint(i, t);

                Gizmos.DrawLine(previousPoint, currentPoint);
                previousPoint = currentPoint;
            }
        }
    }

    private void DrawArrow(Vector3 position, Vector3 direction, float length)
    {
        Vector3 endPoint = position + direction * length;
        Gizmos.DrawLine(position, endPoint);

        // Draw arrowhead
        Vector3 right = Quaternion.Euler(0, 20, 0) * -direction;
        Vector3 left = Quaternion.Euler(0, -20, 0) * -direction;

        Gizmos.DrawLine(endPoint, endPoint + right * (length * 0.3f));
        Gizmos.DrawLine(endPoint, endPoint + left * (length * 0.3f));
    }
}