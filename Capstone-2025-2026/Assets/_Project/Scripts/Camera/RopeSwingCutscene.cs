using System.Collections.Generic;
using UnityEngine;

public class RopeSwingCutscene : PlayerCutsceneBase
{
    [Header("Rope")]
    [SerializeField] private LineRenderer ropeVisuals;
    [SerializeField] private List<Transform> ropePath;

    [Header("Player Offset Path")]
    [SerializeField] private List<Transform> playerOffsetPath;

    [Header("Spline Settings")]
    [SerializeField] private bool useSpline = true;
    [SerializeField, Range(5, 50)] private int splineSubdivisions = 20;

    [Header("Player Sway")]
    [SerializeField] private float swayPositionStrength = 0.02f;
    [SerializeField] private float maxSwayAngle = 15f;
    [SerializeField] private float durationUntilMaxSway = 2f;

    [Header("Rotation Components")]
    [SerializeField] private float lookAheadBuffer = 0.05f;
    [SerializeField] private float ropeVisualLookAhead = 0.1f;
    [SerializeField] private float maxLeanAngle = 45f;
    [SerializeField] private float durationUntilMaxLean = 0.1f;

    [Header("Camera")]
    [SerializeField] private float cameraRotationSpeed = 10f;
    [SerializeField] private float cameraPitch = 5f;

    [Header("Particle System")]
    [SerializeField] private ParticleSystem sparkParticles;

    [Header("Gizmos")]
    [SerializeField] private Color ropePathColor = new Color(0.2f, 0.6f, 1f, 1f);
    [SerializeField] private Color playerPathColor = new Color(0.2f, 1f, 0.3f, 1f);
    [SerializeField] private LassoVisuals lassoVisuals;

    private List<Vector3> _bakedPlayerPath;
    private float _currentSpeed;
    private float _currentModelYaw;
    private float _currentSwayAngle;
    private float _smoothedCurvature;
    private float _smoothedSpeed;
    private float _startTime;
    private Vector3 _previousPos;
    private Vector3 _attachmentPos;
    private Quaternion _pathEndRotation;

    #region Unity Functions and Cutscene Overrides
    private void Awake()
    {
        DrawRopeVisuals();
    }

    public override void OnCutscenePrepare()
    {
        _bakedPlayerPath = BakePlayerPath();

        Vector3 endFwd = GetPathForward(1f, 0f);
        Vector3 endFwdFlat = new Vector3(endFwd.x, 0f, endFwd.z);
        _pathEndRotation = endFwdFlat.sqrMagnitude > 0.01f ? Quaternion.LookRotation(endFwdFlat) : Quaternion.identity;
        CameraRefData.Instance.CameraCutsceneHandler.SetCameraPositionDamping(Vector3.zero);

        base.OnCutscenePrepare();
    }

    public override void OnCutsceneStart()
    {
        base.OnCutsceneStart();

        _startTime = Time.time;
        _smoothedCurvature = 0f;
        _smoothedSpeed = 0f;
        _currentSwayAngle = 0f;
        _previousPos = GetPlayerPosition(0f);

        Vector3 startFwd = GetPathForward(0f, lookAheadBuffer);
        Vector3 startFwdFlat = new Vector3(startFwd.x, 0f, startFwd.z);
        _currentModelYaw = startFwdFlat.sqrMagnitude > 0.01f ? Quaternion.LookRotation(startFwdFlat).eulerAngles.y : 0f;

        PlayerActions.Instance.RumbleFor(0.015f, 0.03f, Duration);
        PlayerRefData.Instance.LassoTetherController.ClearHold();

        if (playerModelRotation != null)
            playerModelRotation.SetNewRotationDir(null, true);

        if (sparkParticles != null)
        {
            if (sparkParticles.isPlaying) sparkParticles.Stop();
            var main = sparkParticles.main;
            main.duration = Duration;
            sparkParticles.Play();
        }
    }

    public override void OnCutsceneTick()
    {
        base.OnCutsceneTick();

        Vector3 basePos = GetPlayerPosition(T);
        Vector3 forward = GetPathForward(T, lookAheadBuffer);
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 swayPos = basePos - right * (_currentSwayAngle * swayPositionStrength);

        playerRb.MovePosition(swayPos);
        playerRb.MoveRotation(GetPlayerBodyRotation(T, lookAheadBuffer));

        _currentSpeed = Vector3.Distance(basePos, _previousPos) / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
        _previousPos = basePos;

        UpdateSway(T, _currentSpeed);

        float blendOutStart = 1f - (BlendOutTime / Duration);
        if (T >= blendOutStart)
        {
            float blendT = Mathf.Clamp01((T - blendOutStart) / (BlendOutTime / Duration));
            var (lassoScreen, lassoTarget, lassoDistance) = CameraRefData.Instance.CameraModeController.GetCurrentStateOffsets();
            CameraRefData.Instance.CameraCutsceneHandler?.SetCameraScreenOffsetDirect(Vector2.Lerp(cameraScreenOffset, lassoScreen, blendT));
            CameraRefData.Instance.CameraCutsceneHandler?.SetCameraTargetOffsetDirect(Vector3.Lerp(cameraTargetOffset, lassoTarget, blendT));
            CameraRefData.Instance.CameraCutsceneHandler?.SetCameraZOffsetDirect(Mathf.Lerp(cameraZOffset, lassoDistance, blendT));
        }
    }

    public override void OnCutsceneSkip()
    {
        Vector3 endPos = GetPlayerPosition(1f);
        if (playerRb != null)
            playerRb.position = endPos;

        if (playerModelRotation != null)
        {
            playerModelRotation.SetSwayAngle(_pathEndRotation.eulerAngles.y, 0f);
            playerModelRotation.SetLeanAngle(0f);
        }
    }

    public override void OnCutsceneEnd()
    {
        base.OnCutsceneEnd();

        if (lassoVisuals != null)
            lassoVisuals.ExitCutsceneMode();

        Vector3 finalForward = GetPathForward(1f, 0f);
        if (finalForward.sqrMagnitude > 0.001f)
            playerModelRotation.SetNewRotationDir(Quaternion.LookRotation(finalForward), false);

        _bakedPlayerPath = null;
        CameraRefData.Instance.CameraCutsceneHandler?.SetCameraPositionDamping(null);
    }

    public override void OnCutsceneLateUpdate()
    {
        if (BlendDelayActive || !IsPlaying) return;

        Vector3 renderPos = GetRenderPlayerPosition();
        _attachmentPos = renderPos - SampleOffsetInPathSpace(T);

        if (lassoVisuals != null)
            lassoVisuals.DrawCutsceneRopeExternal(lassoVisuals.GetHoldPos(), _attachmentPos);

        DrawRopeVisuals();

        if (sparkParticles != null)
        {
            sparkParticles.transform.position = _attachmentPos;
            sparkParticles.transform.rotation = Quaternion.LookRotation(-GetPathForward(T, ropeVisualLookAhead));
        }

        if (IsPlaying)
        {
            Vector3 dir = GetHorizontalDirection(T, lookAheadBuffer);
            if (dir.sqrMagnitude > 0.01f)
                CameraRefData.Instance.CameraCutsceneHandler.RotateCameraToDirection(dir, cameraRotationSpeed, cameraPitch);
        }
    }
    #endregion

    #region Helpers - Rotation
    private Vector3 GetPathForward(float t, float lookAhead)
    {
        //extrapolate ever so slightly forward continuously so it's not 'snapping' to the next spline forward
        //makes it a bit smoother
        float t2 = Mathf.Clamp01(t + lookAhead);
        Vector3 cur = GetPlayerPosition(t);
        Vector3 fwd = GetPlayerPosition(t2);
        Vector3 dir = (fwd - cur).normalized;

        if (dir.sqrMagnitude < 0.01f && _bakedPlayerPath != null && _bakedPlayerPath.Count >= 2)
            dir = (_bakedPlayerPath[_bakedPlayerPath.Count - 1] - _bakedPlayerPath[_bakedPlayerPath.Count - 2]).normalized;

        return dir;
    }

    private Quaternion GetPlayerBodyRotation(float t, float lookAhead)
    {
        Vector3 dir = GetHorizontalDirection(t, lookAhead);
        Quaternion baseRot = dir.sqrMagnitude > 0.01f
            ? Quaternion.LookRotation(dir)
            : Quaternion.identity;

        float blendStart = 1f - BlendOutTime;
        if (t >= blendStart)
        {
            float endT = Mathf.Clamp01((t - blendStart) / BlendOutTime);
            return Quaternion.Slerp(baseRot, _pathEndRotation, endT);
        }

        return baseRot;
    }

    private void UpdateSway(float t, float frameSpeed)
    {
        // --- Signed curvature for side tilt ---
        float lookDelta = 0.05f;
        float tBehind = Mathf.Clamp01(t - lookDelta);
        Vector3 curDir = GetHorizontalDirection(t, lookAheadBuffer);
        Vector3 prevDir = GetHorizontalDirection(tBehind, lookAheadBuffer);

        float curvature = 0f;
        if (curDir.sqrMagnitude > 0.01f && prevDir.sqrMagnitude > 0.01f)
        {
            Vector3 cross = Vector3.Cross(prevDir, curDir);
            curvature = cross.y / lookDelta;
        }

        float swayRate = 1f - Mathf.Exp(-Time.fixedDeltaTime / Mathf.Max(durationUntilMaxSway, 0.001f));
        _smoothedCurvature = Mathf.Lerp(_smoothedCurvature, curvature, swayRate);

        // --- Normalized speed for forward lean ---
        float arcLength = EstimateArcLength(20);
        float peakSpeed = arcLength / Mathf.Max(Duration, 0.001f);
        float normSpeed = Mathf.Clamp01(frameSpeed / Mathf.Max(peakSpeed, 0.001f));

        float leanRate = 1f - Mathf.Exp(-Time.fixedDeltaTime / Mathf.Max(durationUntilMaxLean, 0.001f));
        _smoothedSpeed = Mathf.Lerp(_smoothedSpeed, normSpeed, leanRate);

        // --- Upright fade at end ---
        float blendStart = 1f - BlendOutTime;
        float uprightFade = t >= blendStart
            ? Mathf.Clamp01((t - blendStart) / BlendOutTime)
            : 0f;

        // --- Final angles ---
        _currentSwayAngle = Mathf.Clamp(_smoothedCurvature * maxSwayAngle, -maxSwayAngle, maxSwayAngle)
                            * (1f - uprightFade);
        float leanAngle = _smoothedSpeed * maxLeanAngle * (1f - uprightFade);

        // --- Model yaw ---
        Vector3 dir = GetHorizontalDirection(t, lookAheadBuffer);
        if (t >= blendStart)
        {
            float yawBlendT = Mathf.Clamp01((t - blendStart) / BlendOutTime);
            // _pathEndRotation needs to be stored at prepare time - see below
            _currentModelYaw = Mathf.LerpAngle(_currentModelYaw, _pathEndRotation.eulerAngles.y, yawBlendT);
        }
        else
        {
            _currentModelYaw = Mathf.LerpAngle(_currentModelYaw, Quaternion.LookRotation(dir).eulerAngles.y, 0.3f);
        }

        playerModelRotation.SetSwayAngle(_currentModelYaw, _currentSwayAngle);
        playerModelRotation.SetLeanAngle(leanAngle);
    }
    private Vector3 GetHorizontalDirection(float t, float lookAhead)
    {
        Vector3 dir = GetPathForward(t, lookAhead);
        Vector3 h = new Vector3(dir.x, 0f, dir.z);
        return h.sqrMagnitude > 0.01f ? h.normalized : Vector3.zero;
    }

    private float EstimateArcLength(int samples)
    {
        float arc = 0f;
        for (int i = 0; i < samples; i++)
            arc += Vector3.Distance(
                GetPlayerPosition(i / (float)samples),
                GetPlayerPosition((i + 1) / (float)samples));
        return arc;
    }

    #endregion

    #region Helpers - Get positions on splines

    //helper to get position on the rope path at a given segment and t value
    private Vector3 SampleRopePath(int segmentIndex, float t)
    {
        //no spline
        if (!useSpline || ropePath.Count < 3)
            return Vector3.Lerp(ropePath[segmentIndex].position, ropePath[segmentIndex + 1].position, t);

        //gets the 4 points needed for catmull-rom
        //4 points are needed because the curve is influenced by the point before and after the current segment (current segment is between segmentIndex and segmentIndex + 1)
        //-1 gives the previous neighbour to calculate tangent, +2 gives the next neighbour for the same thing
        Vector3 p0 = GetRopePoint(segmentIndex - 1);
        Vector3 p1 = GetRopePoint(segmentIndex);
        Vector3 p2 = GetRopePoint(segmentIndex + 1);
        Vector3 p3 = GetRopePoint(segmentIndex + 2);

        return CatmullRom(p0, p1, p2, p3, t);
    }


    //helper to get player offset position at a given t value
    private Vector3 SampleOffsetAtT(float t)
    {
        //no offset or only 1 point
        if (playerOffsetPath == null || playerOffsetPath.Count == 0)
            return Vector3.zero;

        Transform offsetParent = playerOffsetPath[0].parent;

        if (playerOffsetPath.Count == 1)
        {
            Vector3 localOffset = playerOffsetPath[0].localPosition;
            return offsetParent != null ? offsetParent.TransformVector(localOffset) : localOffset;
        }

        //get the corresponding segment based on t, similar to how we do it for the rope path
        //but, the offset path might have a different number of points than the rope path
        //this just handles finding the two points in the offset path that t falls between and lerping between them
        int segments = playerOffsetPath.Count - 1;
        float scaled = t * segments;
        int segIndex = Mathf.Clamp(Mathf.FloorToInt(scaled), 0, segments - 1);
        float localT = scaled - segIndex;

        Vector3 localSpaceOffset;
        if (!useSpline || playerOffsetPath.Count < 3)
            localSpaceOffset = Vector3.Lerp(playerOffsetPath[segIndex].localPosition, playerOffsetPath[segIndex + 1].localPosition, localT);
        else
        {
            Vector3 p0 = GetOffsetPoint(segIndex - 1);
            Vector3 p1 = GetOffsetPoint(segIndex);
            Vector3 p2 = GetOffsetPoint(segIndex + 1);
            Vector3 p3 = GetOffsetPoint(segIndex + 2);
            localSpaceOffset = CatmullRom(p0, p1, p2, p3, localT);
        }

        //transform the interpolated local-space offset into world space
        return offsetParent != null ? offsetParent.TransformVector(localSpaceOffset) : localSpaceOffset;
    }

    private Vector3 SampleOffsetInPathSpace(float t)
    {
        if (playerOffsetPath == null || playerOffsetPath.Count == 0)
            return Vector3.zero;

        // Get the raw offset value (in the offset path's local space)
        int segments = playerOffsetPath.Count - 1;
        float scaled = t * segments;
        int segIndex = Mathf.Clamp(Mathf.FloorToInt(scaled), 0, segments - 1);
        float localT = scaled - segIndex;

        Vector3 localOffset;
        if (!useSpline || playerOffsetPath.Count < 3)
            localOffset = Vector3.Lerp(playerOffsetPath[segIndex].localPosition, playerOffsetPath[segIndex + 1].localPosition, localT);
        else
        {
            Vector3 p0 = GetOffsetPoint(segIndex - 1);
            Vector3 p1 = GetOffsetPoint(segIndex);
            Vector3 p2 = GetOffsetPoint(segIndex + 1);
            Vector3 p3 = GetOffsetPoint(segIndex + 2);
            localOffset = CatmullRom(p0, p1, p2, p3, localT);
        }

        // Build a coordinate frame from the rope's tangent at this t
        float t2 = Mathf.Clamp01(t + 0.01f);
        int seg1 = Mathf.Clamp(Mathf.FloorToInt(t * (ropePath.Count - 1)), 0, ropePath.Count - 2);
        int seg2 = Mathf.Clamp(Mathf.FloorToInt(t2 * (ropePath.Count - 1)), 0, ropePath.Count - 2);
        float lt1 = t * (ropePath.Count - 1) - seg1;
        float lt2 = t2 * (ropePath.Count - 1) - seg2;

        Vector3 pos1 = SampleRopePath(seg1, lt1);
        Vector3 pos2 = SampleRopePath(seg2, lt2);
        Vector3 forward = (pos2 - pos1);
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        forward.Normalize();

        Quaternion pathRot = Quaternion.LookRotation(forward, Vector3.up);
        return pathRot * localOffset;
    }
    #endregion

    #region Helpers - Raw points from path lists
    private Vector3 GetRopePoint(int index)
    {
        //for points outside the range of the rope path, extrapolate based on the first or last two points
        //this uses the two points to create a direction vector and extends it out the same distance again to simulate a natural continuation
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

        //otherwise just get the point on the spline
        return ropePath[index].position;
    }


    //same idea as the rope
    private Vector3 GetOffsetPoint(int index)
    {
        //extrapolate
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
    #endregion

    #region Visuals - Rope + Lasso
    private void DrawRopeVisuals()
    {
        if (ropeVisuals == null || ropePath == null || ropePath.Count < 2) return;

        var positions = new List<Vector3>(ropePath.Count);
        foreach (var point in ropePath)
            if (point != null) positions.Add(point.position);

        Vector3[] smoothed = LineSmoother.SmoothLine(positions.ToArray(), 0.1f);
        ropeVisuals.positionCount = smoothed.Length;
        ropeVisuals.SetPositions(smoothed);
        ropeVisuals.startWidth = 0.1f;
        ropeVisuals.endWidth = 0.1f;
    }

    private Vector3 GetRenderPlayerPosition()
    {
        float elapsed = Time.time - _startTime;
        float t = Mathf.Clamp01(elapsed / Duration);
        return GetPlayerPosition(t);
    }

    #endregion

    #region Spline function
    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return p1
             + 0.5f * (p2 - p0) * t
             + 0.5f * (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
             + 0.5f * (-p0 + 3f * p1 - 3f * p2 + p3) * t3;
    }

    #endregion

    #region Player position - Path baking + accessor

    //this just calculates the player's path based on the offset
    //only needs to happen once at the start for performance
    private List<Vector3> BakePlayerPath()
    {
        int totalSegments = ropePath.Count - 1;
        var baked = new List<Vector3>(totalSegments * splineSubdivisions + 1);

        for (int i = 0; i < totalSegments; i++)
        {
            for (int j = 0; j < splineSubdivisions; j++)
            {
                float localT = j / (float)splineSubdivisions;
                float globalT = (i + localT) / totalSegments;
                Vector3 ropePos = SampleRopePath(i, localT);
                baked.Add(ropePos + SampleOffsetInPathSpace(globalT));
            }
        }

        baked.Add(ropePath[ropePath.Count - 1].position + SampleOffsetInPathSpace(1f));
        return baked;
    }

    public override Vector3 GetPlayerPosition(float t)
    {
        if (_bakedPlayerPath == null || _bakedPlayerPath.Count == 0)
            return Vector3.zero;

        //get the corresponding index in the baked path based on t
        //floorToInt will give us the lower index
        float index = t * (_bakedPlayerPath.Count - 1);
        int i = Mathf.FloorToInt(index);

        //when t = 1, index will be exactly the last element
        if (i >= _bakedPlayerPath.Count - 1)
            return _bakedPlayerPath[_bakedPlayerPath.Count - 1];

        //lerp between the two points that t falls between in the baked path to get the current pos
        return Vector3.Lerp(_bakedPlayerPath[i], _bakedPlayerPath[i + 1], index - i);
    }
    #endregion

    #region Gizmos

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        DrawRopePathGizmo();
        DrawPlayerPathGizmo();
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
                Vector3 next = canSpline ? SampleRopePath(i, lt) : ropePath[i + 1].position;
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }

    private void DrawPlayerPathGizmo()
    {
        if (ropePath == null || ropePath.Count < 2) return;

        Gizmos.color = playerPathColor;
        int totalSegments = ropePath.Count - 1;
        Vector3? prev = null;

        for (int seg = 0; seg < totalSegments; seg++)
        {
            for (int j = 0; j < splineSubdivisions; j++)
            {
                float localT = j / (float)splineSubdivisions;
                float globalT = (seg + localT) / totalSegments;
                Vector3 combined = SampleRopePath(seg, localT) + SampleOffsetInPathSpace(globalT);

                if (prev.HasValue) Gizmos.DrawLine(prev.Value, combined);
                prev = combined;
            }
        }

        if (ropePath[ropePath.Count - 1] != null)
        {
            Vector3 last = ropePath[ropePath.Count - 1].position + SampleOffsetInPathSpace(1f);
            if (prev.HasValue) Gizmos.DrawLine(prev.Value, last);
        }
    }
    #endif
    #endregion
}