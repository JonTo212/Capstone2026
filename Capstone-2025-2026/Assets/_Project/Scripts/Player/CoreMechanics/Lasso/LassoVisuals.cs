using NUnit.Framework;
using System.Net;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LassoVisuals : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Lasso lassoScript;
    [SerializeField] private LassoTetherController lassoController;
    [SerializeField] private PlayerActions playerActions;
    [SerializeField] private Transform lassoPointVisuals;

    [Header("Spring Wave Values")]
    [SerializeField] private int ropeSegmentCount = 50; // reduced for performance
    [SerializeField] private float damper = 15f;
    [SerializeField] private float strength = 800f;
    [SerializeField] private float velocity = 15f;
    [SerializeField] private float waveCount = 3f;
    [SerializeField] private float waveHeight = 2f;
    [SerializeField] private AnimationCurve affectCurve;

    [Header("Bend Values")]
    [SerializeField] private float bendScale = 0.5f;
    [SerializeField] private float minBend = 0f;
    [SerializeField] private float maxBend = 2.5f;

    private LineRenderer lineRenderer;
    private Material lineRendererMat;
    private Spring spring;
    private Vector3 currentPullPos;
    private Vector3 lastMousePosition;
    private bool isSpringSettled;
    private bool hasMouseMoved = false;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRendererMat = lineRenderer.material;
        lassoPointVisuals.gameObject.SetActive(false);
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
            lassoPointVisuals.gameObject.SetActive(false);
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
            bool shouldDrawBendyRope = isSpringSettled || hasMouseMoved;

            if (shouldDrawBendyRope)
            {
                isSpringSettled = true;
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
        bool isHolding = lassoController.CurrentLassoState == LassoState.Held;
        bool isUsing = lassoController.CurrentLassoState == LassoState.Using;
        return isHolding || isUsing;
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

        lassoPointVisuals.gameObject.SetActive(true);
        lassoPointVisuals.transform.position = currentPullPos;
        lassoPointVisuals.transform.rotation = Quaternion.Euler(Vector3.zero);

        for (int i = 0; i < ropeSegmentCount + 1; i++)
        {
            float delta = i / (float)ropeSegmentCount;
            Vector3 offset = up * waveHeight * Mathf.Sin(delta * waveCount * Mathf.PI) * spring.Value * affectCurve.Evaluate(delta);
            Vector3 ropePos = Vector3.Lerp(startPoint, currentPullPos, delta) + offset;

            lineRenderer.SetPosition(i, ropePos);
        }
    }

    private void DrawBendyRope()
    {
        if (lassoScript == null || lineRenderer == null) return;

        Vector3 startPoint = lassoScript.HoldPos.position;
        Vector3 endPoint = lassoScript.HitPos;
        Camera cam = lassoScript.PlayerCam;

        //recalculate object depth relative to camera -> this is for tethered objects that move
        //using GetCenterOfScreen() doesn't work because that uses a stale _anchorDist value
        Vector3 cameraToObject = endPoint - cam.transform.position;
        float objectDepth = Vector3.Dot(cameraToObject, cam.transform.forward);
        Vector3 dynamicCenterPoint = cam.transform.position + cam.transform.forward * objectDepth;
        float totalDistance = Vector3.Distance(dynamicCenterPoint, endPoint);

        //get middle of screen + object hit point and convert to screen space
        //then, find the opposite vector of the direction vector between the two
        Vector3 screenStart = Camera.main.WorldToScreenPoint(dynamicCenterPoint);
        Vector3 screenEnd = Camera.main.WorldToScreenPoint(endPoint);
        Vector3 screenDirection = (screenEnd - screenStart).normalized;
        Vector3 screenPerpendicular = new Vector3(-screenDirection.x, -screenDirection.y, 0f);

        //convert the opposite vector back to world space
        Vector3 worldPerpendicular = Camera.main.transform.TransformDirection(screenPerpendicular);
        Vector3 combinedBendAxis = worldPerpendicular.normalized;

        //one point at midpoint, one at 3/4
        Vector3 controlPoint1 = Vector3.Lerp(startPoint, endPoint, 0.175f);
        Vector3 controlPoint2 = Vector3.Lerp(startPoint, endPoint, 0.35f);
        Vector3 controlPoint3 = Vector3.Lerp(startPoint, endPoint, 0.525f);
        Vector3 controlPoint4 = Vector3.Lerp(startPoint, endPoint, 0.7f);
        Vector3 controlPoint5 = Vector3.Lerp(startPoint, endPoint, 0.875f);

        //determine how much the object can bend
        float currentBendOffset = Mathf.Clamp(totalDistance * bendScale, minBend, maxBend);
        controlPoint1 += combinedBendAxis * currentBendOffset;
        controlPoint2 += combinedBendAxis * currentBendOffset; 

        Vector3[] linePositions = new Vector3[7]
        {  startPoint, controlPoint1, controlPoint2, controlPoint3, controlPoint4, controlPoint5, endPoint };

        Vector3[] smoothedPoints = LineSmoother.SmoothLine(linePositions, 0.1f);

        lineRenderer.positionCount = smoothedPoints.Length;
        lineRenderer.SetPositions(smoothedPoints);
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;

        lassoPointVisuals.gameObject.SetActive(true);
        currentPullPos = lassoScript.HitPos;
        lassoPointVisuals.transform.position = currentPullPos;
        lassoPointVisuals.transform.rotation = Quaternion.Euler(Vector3.zero);
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
