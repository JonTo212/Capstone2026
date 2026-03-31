using UnityEngine;

/// <summary>
/// A first-person escape pod crash cutscene.
/// The camera IS the pod — it follows a Catmull-Rom spline path downward,
/// wobbles slightly as it falls, then triggers screenshake + controller rumble
/// on impact and ends once the shake settles.
///
/// Setup:
///   - Assign startPos, endPos, and any midPoints to define the crash arc.
///   - Set impactT (0–1) to mark when the pod hits the ground.
///   - Tune wobbleStrength, wobbleFrequency, and wobbleRampDuration for feel.
///   - Set shakeDuration and shakeMagnitude for the impact hit.
///   - Optionally assign a nextCutscene to chain after this one ends.
/// </summary>
public class EscapePodCrashCutscene : CameraCutsceneBase
{
    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Header("Path")]
    [SerializeField] private Transform[] midPoints;

    [Header("Impact")]
    [Tooltip("Normalised time (0–1) at which the pod hits the ground.")]
    [SerializeField, Range(0f, 1f)] private float impactT = 0.85f;

    [Header("Wobble (pre-impact)")]
    [Tooltip("Max roll/pitch offset in degrees.")]
    [SerializeField] private float wobbleStrength = 4f;
    [Tooltip("Oscillations per second.")]
    [SerializeField] private float wobbleFrequency = 1.2f;
    [Tooltip("How many seconds it takes for wobble to ramp up to full strength.")]
    [SerializeField] private float wobbleRampDuration = 1.5f;

    [Header("Screenshake")]
    [SerializeField] private float shakeMagnitude = 0.35f;
    [SerializeField] private float shakeDuration = 1.1f;

    [Header("Controller Rumble")]
    [SerializeField] private float rumbleLow = 0.6f;
    [SerializeField] private float rumbleHigh = 0.9f;

    [Header("Chaining")]
    [SerializeField] private CutsceneBase nextCutscene;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private Transform[] _allPoints;

    private bool _impactFired = false;
    private float _shakeElapsed = 0f;
    private bool _shaking = false;

    private Vector3 _shakeOffset = Vector3.zero;
    private Vector3 _settledPosition;
    private Quaternion _settledRotation;

    // -------------------------------------------------------------------------
    // CutsceneBase overrides
    // -------------------------------------------------------------------------

    public override void OnCutscenePrepare()
    {
        base.OnCutscenePrepare();
        BuildPointArray();
    }

    public override void OnCutsceneStart()
    {
        base.OnCutsceneStart();

        _impactFired = false;
        _shaking = false;
        _shakeElapsed = 0f;
        _shakeOffset = Vector3.zero;

        // Disable player scripts / objects defined in CameraCutsceneBase
        HandleScripts(false);

        // Snap camera to the start of the path immediately
        cam.transform.position = GetCatmullRomPosition(0f);
        cam.transform.rotation = GetPathRotation(0f, 0f);
    }

    public override void OnCutsceneTick()
    {
        base.OnCutsceneTick();

        // --- Fire impact once ---
        if (!_impactFired && T >= impactT)
        {
            FireImpact();
        }

        // --- Pre-impact: move along path with wobble ---
        if (!_impactFired)
        {
            cam.transform.position = GetCatmullRomPosition(T);
            cam.transform.rotation = GetPathRotation(T, Time.time);
            return;
        }

        // --- Post-impact: hold settled position, apply decaying shake ---
        if (_shaking)
        {
            _shakeElapsed += Time.fixedDeltaTime;
            float progress = Mathf.Clamp01(_shakeElapsed / shakeDuration);
            float envelope = 1f - Mathf.SmoothStep(0f, 1f, progress);

            _shakeOffset = Random.insideUnitSphere * (shakeMagnitude * envelope);
            cam.transform.position = _settledPosition + _shakeOffset;
            cam.transform.rotation = _settledRotation;

            // Shake finished — end the cutscene
            if (progress >= 1f)
            {
                _shaking = false;
                cam.transform.position = _settledPosition;
            }
        }
    }

    public override void OnCutsceneEnd()
    {
        base.OnCutsceneEnd();

        cam.transform.position = endPos.position;
        cam.transform.rotation = GetPathRotation(1f, 0f);

        HandleScripts(true);

        if (nextCutscene != null)
            CameraCutsceneHandler.Instance.StartCutscene(nextCutscene);
    }

    public override void OnCutsceneSkip()
    {
        _impactFired = false;
        _shaking = false;
        HandleScripts(true);
    }

    // -------------------------------------------------------------------------
    // Impact
    // -------------------------------------------------------------------------

    private void FireImpact()
    {
        _impactFired = true;
        _shaking = true;
        _shakeElapsed = 0f;

        // Snapshot the settled camera state (end of path, facing lookAt)
        _settledPosition = endPos.position;
        _settledRotation = lookAtTarget != null
            ? Quaternion.LookRotation(lookAtTarget.position - endPos.position)
            : GetPathRotation(1f, 0f);

        // Snap camera there instantly — shake plays from this anchor
        cam.transform.position = _settledPosition;
        cam.transform.rotation = _settledRotation;

        // Controller rumble (uses same API as RopeSwingCutscene)
        PlayerActions.Instance?.RumbleFor(rumbleLow, rumbleHigh, shakeDuration);

        // Tell the handler the cutscene duration is now just the shake window
        // We manually end via _shaking flag in OnCutsceneTick.
    }

    // -------------------------------------------------------------------------
    // Path helpers
    // -------------------------------------------------------------------------

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

    /// <summary>
    /// Returns a rotation that faces the spline's forward direction,
    /// plus a time-driven wobble that ramps up as the pod accelerates.
    /// </summary>
    private Quaternion GetPathRotation(float t, float time)
    {
        // Forward direction along the spline
        float tAhead = Mathf.Clamp01(t + 0.02f);
        Vector3 forward = (GetCatmullRomPosition(tAhead) - GetCatmullRomPosition(t)).normalized;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;

        Quaternion baseRot = Quaternion.LookRotation(forward);

        // Wobble ramps in over wobbleRampDuration seconds and fades out near impact
        float elapsed = t * Duration;
        float ramp = Mathf.Clamp01(elapsed / Mathf.Max(wobbleRampDuration, 0.01f));
        float fadeOut = Mathf.Clamp01((impactT - t) / 0.1f); // fade wobble in the last 10% before impact
        float strength = wobbleStrength * ramp * fadeOut;

        float roll = Mathf.Sin(time * wobbleFrequency * Mathf.PI * 2f) * strength;
        float pitch = Mathf.Sin(time * wobbleFrequency * Mathf.PI * 2f * 0.7f + 1.3f) * (strength * 0.5f);

        Quaternion wobble = Quaternion.Euler(pitch, 0f, roll);
        return baseRot * wobble;
    }

    // -------------------------------------------------------------------------
    // Editor gizmos
    // -------------------------------------------------------------------------

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (startPos == null || endPos == null) return;

        BuildPointArray();
        if (_allPoints.Length < 2) return;

        // Path line
        Gizmos.color = Color.cyan;
        int resolution = 60;
        for (int i = 0; i < resolution; i++)
        {
            float t1 = (float)i / resolution;
            float t2 = (float)(i + 1) / resolution;
            Gizmos.DrawLine(GetCatmullRomPosition(t1), GetCatmullRomPosition(t2));
        }

        // Start / end / mid markers
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(startPos.position, 0.2f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(endPos.position, 0.2f);

        Gizmos.color = Color.yellow;
        if (midPoints != null)
            foreach (var mp in midPoints)
                if (mp != null) Gizmos.DrawSphere(mp.position, 0.15f);

        // Impact point
        Gizmos.color = new Color(1f, 0.4f, 0f);
        Gizmos.DrawSphere(GetCatmullRomPosition(impactT), 0.3f);
        UnityEditor.Handles.Label(GetCatmullRomPosition(impactT) + Vector3.up * 0.5f, "IMPACT");
    }
#endif
}