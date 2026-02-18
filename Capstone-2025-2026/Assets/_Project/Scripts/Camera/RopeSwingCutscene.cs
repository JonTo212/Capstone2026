using System.Collections.Generic;
using UnityEngine;

public class RopeSwingCutscene : CutsceneBase
{

    [Header("Visuals")]
    [SerializeField] private LineRenderer ropeVisuals;


    [Header("Rope Path")]
    [Tooltip("Control points that define the rope arc the player swings along.")]
    [SerializeField] private List<Transform> ropePath;

    [Header("Player Offset Path")]
    [Tooltip("Local offsets applied on top of the rope path to position the player's body.")]
    [SerializeField] private List<Transform> playerOffsetPath;

    [Tooltip("When disabled the player sits exactly on the rope (zero offset).")]
    [SerializeField] private bool usePlayerOffset = true;


    [Header("Spline Settings")]
    [Tooltip("Use Catmull-Rom spline for smoother path interpolation.")]
    [SerializeField] private bool useSpline = true;

    [Tooltip("Number of subdivisions per segment when baking the combined spline.")]
    [Range(5, 50)]
    [SerializeField] private int splineSubdivisions = 20;


    [Header("Player Animation")]
    [Tooltip("Apply side-sway tilt to the player model during the swing.")]
    [SerializeField] private bool enablePlayerSway = true;

    [Tooltip("Maximum side-tilt angle in degrees.")]
    [SerializeField] private float maxSwayAngle = 15f;

    [Tooltip("Seconds to reach maximum sway angle.")]
    [SerializeField] private float durationUntilMaxSwayAngle = 0.3f;

    [Tooltip("Maximum forward-lean angle in degrees.")]
    [SerializeField] private float maxLeanAngle = 20f;

    [Tooltip("Seconds to build up to maximum forward lean.")]
    [SerializeField] private float durationUntilMaxLean = 0.4f;

    [Tooltip("Fraction of total duration [0-1] used to blend the player back upright at the end.")]
    [SerializeField][Range(0f, 0.5f)] private float uprightBlendFraction = 0.2f;


    [Header("Visualization")]
    [SerializeField] private bool showPaths = true;

    [Tooltip("Colour of the rope path gizmos.")]
    [SerializeField] private Color ropePathColor = new Color(0.2f, 0.6f, 1f, 1f);

    [Tooltip("Colour of the final combined player world-position preview.")]
    [SerializeField] private Color combinedPathColor = new Color(0.2f, 1f, 0.3f, 1f);


    private List<Vector3> _bakedPath;

    private Quaternion _pathStartRotation;
    private Quaternion _pathEndRotation;

    private float _smoothedCurvature;
    private float _smoothedSpeed;
    private float _currentModelYaw;

    public Quaternion PathStartRotation => _pathStartRotation;
    public Quaternion PathEndRotation => _pathEndRotation;
    public float UprightBlendFraction => uprightBlendFraction;
    public bool EnablePlayerSway => enablePlayerSway;


    public override bool IsValid() =>
        ropePath != null && ropePath.Count >= 2;

    public override void OnCutscenePrepare()
    {
        _bakedPath = BakeCombinedPath();
        _pathStartRotation = DeriveFlatRotation(0f);
        _pathEndRotation = DeriveFlatRotation(1f);
        _smoothedCurvature = 0f;
        _smoothedSpeed = 0f;
        _currentModelYaw = _pathStartRotation.eulerAngles.y;
    }

    public override Vector3 GetPlayerPosition(float t)
    {
        if (_bakedPath == null || _bakedPath.Count == 0)
            return Vector3.zero;

        float index = t * (_bakedPath.Count - 1);
        int i = Mathf.FloorToInt(index);

        if (i >= _bakedPath.Count - 1)
            return _bakedPath[_bakedPath.Count - 1];

        return Vector3.Lerp(_bakedPath[i], _bakedPath[i + 1], index - i);
    }

    public override void OnCutsceneStart()
    {
        _smoothedCurvature = 0f;
        _smoothedSpeed = 0f;
    }

    public void TickAnimation(float t, float frameSpeed,
                              PlayerModelRotationHandler modelRotation)
    {
        if (!enablePlayerSway || modelRotation == null) return;

        float lookDelta = 0.05f;
        float tBehind = Mathf.Clamp01(t - lookDelta);

        Vector3 curDir = GetHorizontalDirection(t);
        Vector3 prevDir = GetHorizontalDirection(tBehind);

        float curvature = 0f;
        if (curDir.sqrMagnitude > 0.01f && prevDir.sqrMagnitude > 0.01f)
        {
            Vector3 cross = Vector3.Cross(prevDir, curDir);
            curvature = cross.y / lookDelta;
        }

        float swayRate = 1f - Mathf.Exp(-Time.fixedDeltaTime / Mathf.Max(durationUntilMaxSwayAngle, 0.001f));
        _smoothedCurvature = Mathf.Lerp(_smoothedCurvature, curvature, swayRate);

        float arcLength = EstimateArcLength(20);
        float peakSpeed = arcLength / Mathf.Max(duration, 0.001f);
        float normSpeed = Mathf.Clamp01(frameSpeed / Mathf.Max(peakSpeed, 0.001f));

        float leanRate = 1f - Mathf.Exp(-Time.fixedDeltaTime / Mathf.Max(durationUntilMaxLean, 0.001f));
        _smoothedSpeed = Mathf.Lerp(_smoothedSpeed, normSpeed, leanRate);

        float blendStart = 1f - uprightBlendFraction;
        float uprightFade = (t >= blendStart)
            ? Mathf.Clamp01((t - blendStart) / uprightBlendFraction)
            : 0f;

        float swayAngle = Mathf.Clamp(_smoothedCurvature * maxSwayAngle, -maxSwayAngle, maxSwayAngle)
                          * (1f - uprightFade);
        float leanAngle = _smoothedSpeed * maxLeanAngle * (1f - uprightFade);
        Vector3 dir = GetHorizontalDirection(t);
        Quaternion faceRot = dir.sqrMagnitude > 0.01f
            ? Quaternion.LookRotation(dir)
            : Quaternion.identity;

        if (t >= blendStart)
        {
            float yawBlendT = Mathf.Clamp01((t - blendStart) / uprightBlendFraction);
            _currentModelYaw = Mathf.LerpAngle(_currentModelYaw, _pathEndRotation.eulerAngles.y, yawBlendT);
        }
        else
        {
            _currentModelYaw = Mathf.LerpAngle(_currentModelYaw, faceRot.eulerAngles.y, 0.3f);
        }

        modelRotation.SetSwayAngle(_currentModelYaw, swayAngle);
        modelRotation.SetLeanAngle(leanAngle);
    }

    public Quaternion GetPlayerBodyRotation(float t)
    {
        Vector3 dir = GetHorizontalDirection(t);
        Quaternion baseRot = dir.sqrMagnitude > 0.01f
            ? Quaternion.LookRotation(dir)
            : Quaternion.identity;

        float blendStart = 1f - uprightBlendFraction;
        if (t >= blendStart)
        {
            float endT = Mathf.Clamp01((t - blendStart) / uprightBlendFraction);
            return Quaternion.Slerp(baseRot, _pathEndRotation, endT);
        }

        return baseRot;
    }

    public override void OnCutsceneEnd()
    {
        _bakedPath = null;
    }

    public override void OnCutsceneLateUpdate()
    {
        DrawRopeVisuals();
    }
    public Vector3 SampleOffsetAtT(float t)
    {
        if (!usePlayerOffset || playerOffsetPath == null || playerOffsetPath.Count == 0)
            return Vector3.zero;

        if (playerOffsetPath.Count == 1)
            return playerOffsetPath[0].localPosition;

        int segments = playerOffsetPath.Count - 1;
        float scaled = t * segments;
        int segIndex = Mathf.Clamp(Mathf.FloorToInt(scaled), 0, segments - 1);
        float localT = scaled - segIndex;

        if (!useSpline || playerOffsetPath.Count < 3)
            return Vector3.Lerp(playerOffsetPath[segIndex].localPosition,
                                playerOffsetPath[segIndex + 1].localPosition, localT);

        Vector3 p0 = GetOffsetPoint(segIndex - 1);
        Vector3 p1 = GetOffsetPoint(segIndex);
        Vector3 p2 = GetOffsetPoint(segIndex + 1);
        Vector3 p3 = GetOffsetPoint(segIndex + 2);

        return CatmullRom(p0, p1, p2, p3, localT);
    }

    public Vector3 GetPathDirection(float t)
    {
        float t2 = Mathf.Clamp01(t + 0.05f);
        Vector3 cur = GetPlayerPosition(t);
        Vector3 fwd = GetPlayerPosition(t2);
        Vector3 dir = (fwd - cur).normalized;

        if (dir.sqrMagnitude < 0.01f && _bakedPath != null && _bakedPath.Count >= 2)
            dir = (_bakedPath[_bakedPath.Count - 1] - _bakedPath[_bakedPath.Count - 2]).normalized;

        return dir;
    }


    private List<Vector3> BakeCombinedPath()
    {
        int totalSegments = ropePath.Count - 1;
        var combined = new List<Vector3>(totalSegments * splineSubdivisions + 1);

        for (int i = 0; i < totalSegments; i++)
        {
            for (int j = 0; j < splineSubdivisions; j++)
            {
                float localT = j / (float)splineSubdivisions;
                float globalT = (i + localT) / totalSegments;

                Vector3 ropePoint = SampleRopePath(i, localT);
                Vector3 offset = SampleOffsetAtT(globalT);
                combined.Add(ropePoint + offset);
            }
        }

        // Final point
        combined.Add(ropePath[ropePath.Count - 1].position + SampleOffsetAtT(1f));
        return combined;
    }

    private Vector3 SampleRopePath(int segmentIndex, float localT)
    {
        if (!useSpline || ropePath.Count < 3)
            return Vector3.Lerp(ropePath[segmentIndex].position,
                                ropePath[segmentIndex + 1].position, localT);

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
            Vector3 p1 = ropePath[0].position;
            Vector3 p2 = ropePath[1].position;
            return p1 + (p1 - p2);
        }
        if (index >= ropePath.Count)
        {
            int last = ropePath.Count - 1;
            Vector3 pA = ropePath[last].position;
            Vector3 pB = ropePath[last - 1].position;
            return pA + (pA - pB);
        }
        return ropePath[index].position;
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

    private Vector3 GetHorizontalDirection(float t)
    {
        Vector3 dir = GetPathDirection(t);
        Vector3 h = new Vector3(dir.x, 0f, dir.z);
        return h.sqrMagnitude > 0.01f ? h.normalized : Vector3.zero;
    }

    private Quaternion DeriveFlatRotation(float t)
    {
        Vector3 h = GetHorizontalDirection(t);
        return h.sqrMagnitude > 0.01f ? Quaternion.LookRotation(h) : Quaternion.identity;
    }

    private float EstimateArcLength(int samples)
    {
        float arc = 0f;
        for (int i = 0; i < samples; i++)
            arc += Vector3.Distance(GetPlayerPosition(i / (float)samples),
                                    GetPlayerPosition((i + 1) / (float)samples));
        return arc;
    }


    private void DrawRopeVisuals()
    {
        if (ropeVisuals == null || ropePath == null || ropePath.Count < 2) return;

        var positions = new List<Vector3>(ropePath.Count);
        foreach (var t in ropePath)
            if (t != null) positions.Add(t.position);

        Vector3[] smoothed = LineSmoother.SmoothLine(positions.ToArray(), 0.1f);
        ropeVisuals.positionCount = smoothed.Length;
        ropeVisuals.SetPositions(smoothed);
        ropeVisuals.startWidth = 0.1f;
        ropeVisuals.endWidth = 0.1f;
    }


    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return p1
             + 0.5f * (p2 - p0) * t
             + 0.5f * (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
             + 0.5f * (-p0 + 3f * p1 - 3f * p2 + p3) * t3;
    }


#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showPaths) return;
        DrawRopePathGizmo();
        DrawCombinedPreview();
    }

    private void DrawRopePathGizmo()
    {
        if (ropePath == null || ropePath.Count < 2) return;

        for (int i = 0; i < ropePath.Count; i++)
        {
            if (ropePath[i] == null) continue;

            Gizmos.color = i == 0 ? Color.green
                         : i == ropePath.Count - 1 ? Color.red
                         : ropePathColor;
            Gizmos.DrawSphere(ropePath[i].position, 0.25f);

            if (i < ropePath.Count - 1 && ropePath[i + 1] != null)
                DrawArrow(ropePath[i].position,
                          (ropePath[i + 1].position - ropePath[i].position).normalized,
                          0.5f, ropePathColor);
        }

        Gizmos.color = ropePathColor;
        bool canSpline = useSpline && ropePath.Count >= 3;
        int segs = ropePath.Count - 1;

        for (int i = 0; i < segs; i++)
        {
            Vector3 prev = ropePath[i].position;
            int steps = canSpline ? 20 : 1;
            for (int j = 1; j <= steps; j++)
            {
                float lt = j / (float)steps;
                Vector3 next = canSpline ? SampleRopePath(i, lt)
                                         : ropePath[i + 1].position;
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }

    private void DrawCombinedPreview()
    {
        if (ropePath == null || ropePath.Count < 2) return;

        Gizmos.color = combinedPathColor;
        int totalSegments = ropePath.Count - 1;
        Vector3? prev = null;

        if (usePlayerOffset && playerOffsetPath != null)
        {
            foreach (var op in playerOffsetPath)
            {
                if (op == null) continue;
                int idx = playerOffsetPath.IndexOf(op);
                float gt = playerOffsetPath.Count > 1
                    ? idx / (float)(playerOffsetPath.Count - 1) : 0f;
                int segIdx = Mathf.Clamp(Mathf.FloorToInt(gt * totalSegments), 0, totalSegments - 1);
                float lt = (gt * totalSegments) - segIdx;
                Vector3 rope = SampleRopePath(segIdx, lt);
                Gizmos.DrawSphere(rope + op.localPosition, 0.18f);
            }
        }

        for (int seg = 0; seg < totalSegments; seg++)
        {
            for (int j = 0; j < splineSubdivisions; j++)
            {
                float localT = j / (float)splineSubdivisions;
                float globalT = (seg + localT) / totalSegments;
                Vector3 rope = SampleRopePath(seg, localT);
                Vector3 offset = SampleOffsetAtT(globalT);
                Vector3 combined = rope + offset;

                if (prev.HasValue) Gizmos.DrawLine(prev.Value, combined);
                prev = combined;
            }
        }

        Vector3 last = ropePath[ropePath.Count - 1].position + SampleOffsetAtT(1f);
        if (prev.HasValue) Gizmos.DrawLine(prev.Value, last);
    }

    private static void DrawArrow(Vector3 origin, Vector3 dir, float length, Color color)
    {
        Vector3 tip = origin + dir * length;
        Gizmos.color = color;
        Gizmos.DrawLine(origin, tip);
        Gizmos.DrawLine(tip, tip + Quaternion.Euler(0f, 20f, 0f) * -dir * (length * 0.3f));
        Gizmos.DrawLine(tip, tip + Quaternion.Euler(0f, -20f, 0f) * -dir * (length * 0.3f));
    }
#endif
}