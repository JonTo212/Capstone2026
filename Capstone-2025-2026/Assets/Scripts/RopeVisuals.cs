using UnityEngine;

public class RopeVisuals : MonoBehaviour
{
    [SerializeField] private PullAndThrow pullScript;
    [SerializeField] private PlayerActions playerActions;
    [SerializeField] private int ropeSegmentCount;
    [SerializeField] private float damper;
    [SerializeField] private float strength;
    [SerializeField] private float velocity;
    [SerializeField] private float waveCount;
    [SerializeField] private float waveHeight;
    [SerializeField] private AnimationCurve affectCurve;

    private LineRenderer lineRenderer;
    private Spring spring;
    private Vector3 currentPullPos;

    #region Unity Functions
    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        spring = new Spring();
        spring.SetTarget(0);
    }
    void LateUpdate()
    {
        DrawRope();
    }

    #endregion

    #region Visuals

    private void DrawRope()
    {
        if (!playerActions.PullInput)
        {
            ResetRope();
            return;
        }

        if (lineRenderer.positionCount == 0)
        {
            spring.SetVelocity(velocity);
            lineRenderer.positionCount = ropeSegmentCount + 1;
        }

        spring.SetDamper(damper);
        spring.SetStrength(strength);
        spring.Update(Time.deltaTime);

        Vector3 targetPoint = pullScript.TargetPos;
        Vector3 startPoint = pullScript.HoldPos.position;
        Vector3 up = Quaternion.LookRotation((targetPoint - startPoint).normalized) * Vector3.up;

        currentPullPos = Vector3.Lerp(currentPullPos, targetPoint, Time.deltaTime * 12f);

        for (int i = 0; i < ropeSegmentCount + 1; i++)
        {
            float delta = i / (float)ropeSegmentCount;
            Vector3 offset = up * waveHeight * Mathf.Sin(delta * waveCount * Mathf.PI) * spring.Value * affectCurve.Evaluate(delta);

            lineRenderer.SetPosition(i, Vector3.Lerp(startPoint, targetPoint, delta) + offset);
        }
    }

    private void ResetRope()
    {
        currentPullPos = pullScript.HoldPos.position;
        spring.Reset();
        if (lineRenderer.positionCount > 0)
            lineRenderer.positionCount = 0;
    }

    #endregion
}
