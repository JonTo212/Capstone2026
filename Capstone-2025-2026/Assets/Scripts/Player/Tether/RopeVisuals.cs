using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RopeVisuals : MonoBehaviour
{
    [SerializeField] private Tether tetherScript;
    [SerializeField] private int ropeSegmentCount = 500;
    [SerializeField] private float damper = 15f;
    [SerializeField] private float strength = 800f;
    [SerializeField] private float velocity = 15f;
    [SerializeField] private float waveCount = 3f;
    [SerializeField] private float waveHeight = 2f;
    [SerializeField] private AnimationCurve affectCurve;

    private LineRenderer lineRenderer;
    private Spring spring;
    private Vector3 currentPullPos;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        spring = new Spring();
        spring.SetTarget(0);
    }

    private void LateUpdate()
    {
        DrawRope();
    }

    private void DrawRope()
    {
        //hide rope when tether state is empty
        if (tetherScript.CurrentTetherState == TetherState.Empty)
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

        Vector3 startPoint = tetherScript.HoldPos.position;
        Vector3 targetPoint = tetherScript.AnchoredPos.position;
        Vector3 up = Quaternion.LookRotation((targetPoint - startPoint).normalized) * Vector3.up;

        if (tetherScript.CurrentTetherState == TetherState.Firing)
        {
            currentPullPos = Vector3.Lerp(currentPullPos, targetPoint, Time.deltaTime * velocity);
        }
        else
        {
            currentPullPos = targetPoint;
        }

        for (int i = 0; i < ropeSegmentCount + 1; i++)
        {
            float delta = i / (float)ropeSegmentCount;
            Vector3 offset = up * waveHeight * Mathf.Sin(delta * waveCount * Mathf.PI) * spring.Value * affectCurve.Evaluate(delta);
            Vector3 ropePos = Vector3.Lerp(startPoint, currentPullPos, delta) + offset;

            lineRenderer.SetPosition(i, ropePos);
        }
    }

    private void ResetRope()
    {
        currentPullPos = tetherScript.HoldPos.position;
        spring.Reset();
        if (lineRenderer.positionCount > 0)
            lineRenderer.positionCount = 0;
    }
}
