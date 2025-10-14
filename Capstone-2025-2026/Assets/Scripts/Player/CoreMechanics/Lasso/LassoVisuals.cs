using NUnit.Framework;
using System.Net;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LassoVisuals : MonoBehaviour
{
    [SerializeField] private Lasso lassoScript;
    [SerializeField] private LassoTetherController lassoController;
    [SerializeField] private int ropeSegmentCount = 50; // reduced for performance
    [SerializeField] private float damper = 15f;
    [SerializeField] private float strength = 800f;
    [SerializeField] private float velocity = 15f;
    [SerializeField] private float waveCount = 3f;
    [SerializeField] private float waveHeight = 2f;
    [SerializeField] private AnimationCurve affectCurve;
    [SerializeField] private PlayerActions playerActions;

    private LineRenderer lineRenderer;
    private Spring spring;
    private Vector3 currentPullPos;
    private bool isSpringSettled;

    private Vector3 lastMousePosition;
    private bool hasMouseMoved = false;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        spring = new Spring();
        spring.SetTarget(0);
    }

    private void LateUpdate()
    {
        Vector3 currentMousePosition = playerActions.LookInput;
        hasMouseMoved = (currentMousePosition - lastMousePosition).sqrMagnitude > 1f;
        lastMousePosition = currentMousePosition;

        if (lassoScript.SnaredObject == null || DisableVisuals())
        {
            ResetRope();
            isSpringSettled = false;
            return;
        }

        if (lassoController.CurrentLassoState == LassoState.ObjectYanking)
        {
            ResetRope();
            DrawSnapLasso();
            return;
        }

        if (lassoController.CurrentLassoState != LassoState.Swinging)
        {
            if (hasMouseMoved)
            {
                isSpringSettled = true;
                DrawBendyRope();
            }
            else if (isSpringSettled)
            {
                DrawBendyRope();
            }
            else
            {
                DrawSnapLasso();

                if (Mathf.Abs(spring.Velocity) < 0.0125f)
                {
                    isSpringSettled = true;
                }
            }
        }
        else
        {
            DrawSnapLasso();
        }
    }

    private bool DisableVisuals()
    {
        bool isPlayerYanking = lassoController.CurrentLassoState == LassoState.PlayerYanking;
        bool isHolding = lassoController.CurrentLassoState == LassoState.Held;
        bool isUsing = lassoController.CurrentLassoState == LassoState.Using;

        return isPlayerYanking || isHolding || isUsing;
    }

    private void DrawSnapLasso()
    {

        if (lineRenderer.positionCount == 0)
        {
            spring.SetVelocity(velocity);
            lineRenderer.positionCount = ropeSegmentCount + 1;
        }

        spring.SetDamper(damper);
        spring.SetStrength(strength);
        spring.Update(Time.deltaTime);

        Vector3 startPoint = lassoScript.HoldPos.position;
        Vector3 targetPoint = lassoScript.HitPos;
        Vector3 up = Quaternion.LookRotation((targetPoint - startPoint).normalized) * Vector3.up;

        currentPullPos = lassoScript.HitPos;

        for (int i = 0; i < ropeSegmentCount + 1; i++)
        {
            float delta = i / (float)ropeSegmentCount;
            Vector3 offset = up * waveHeight * Mathf.Sin(delta * waveCount * Mathf.PI) * spring.Value * affectCurve.Evaluate(delta);
            Vector3 ropePos = Vector3.Lerp(startPoint, currentPullPos, delta) + offset;

            lineRenderer.SetPosition(i, ropePos);
        }
    }

    // Add these variables to the top of your class, outside of any method.
    // These allow you to control the sideways bend from the Unity Inspector.
    public float bendAmount = 1.0f; // How much the line bends sideways

    private void DrawBendyRope()
    {
        if (lassoScript == null || lineRenderer == null) return;

        Vector3 startPoint = lassoScript.HoldPos.position;
        Vector3 endPoint = lassoScript.HitPos;
        float totalDistance = Vector3.Distance(lassoScript.GetCenterOfScreen(), endPoint);

        Vector3 screenStart = Camera.main.WorldToScreenPoint(lassoScript.GetCenterOfScreen());
        Vector3 screenEnd = Camera.main.WorldToScreenPoint(endPoint);
        Vector3 screenDirection = (screenEnd - screenStart).normalized;

        Vector3 screenPerpendicular = new Vector3(-screenDirection.x, -screenDirection.y, 0f);

        // Project this back into world space
        Vector3 worldPerpendicular = Camera.main.transform.TransformDirection(screenPerpendicular);
        Vector3 combinedBendAxis = worldPerpendicular.normalized;

        // 1. Calculate two intermediate points (roughly at 1/3 and 2/3 of the way)
        // Interpolate between start and end, then offset them
        Vector3 controlPoint1 = Vector3.Lerp(startPoint, endPoint, 0.5f);
        Vector3 controlPoint2 = Vector3.Lerp(startPoint, endPoint, 0.75f);

        // 2. Add a sideways offset to create the bend.
        // The bend amount can be scaled by the distance to make it more noticeable
        // over longer distances, or kept constant.
        float bendScale = 0.5f;   // how much bend per unit distance
        float minBend = 0f;     // minimum bend amount
        float maxBend = 5f;      // max cap
        float currentBendOffset = Mathf.Clamp(totalDistance * bendScale, minBend, maxBend);


        controlPoint1 += combinedBendAxis * currentBendOffset;
        controlPoint2 += combinedBendAxis * currentBendOffset; // You could offset this by a different amount for a more complex curve


        // 3. Create the input array with points that now form an arc
        Vector3[] linePositions = new Vector3[4]
        {
        startPoint,
        controlPoint1,
        controlPoint2,
        endPoint
        };

        // 4. Pass these arced points to your existing smoother
        Vector3[] smoothedPoints = LineSmoother.SmoothLine(linePositions, 0.1f);

        // Set line settings
        lineRenderer.positionCount = smoothedPoints.Length;
        lineRenderer.SetPositions(smoothedPoints);
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
    }


    private void ResetRope()
    {
        currentPullPos = lassoScript.HoldPos.position;
        spring.Reset();
        isSpringSettled = false;
        if (lineRenderer.positionCount > 0)
            lineRenderer.positionCount = 0;
    }
}
