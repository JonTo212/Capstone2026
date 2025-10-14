using System.Collections;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class JointTetherVisuals : MonoBehaviour
{
    private LineRenderer _lineRenderer;

    [Header("Line Variables")]
    [SerializeField] private float lineSegmentSize = 0.15f;
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private float bendAmount = 0.5f;
    [SerializeField] Color activatedStateColor = Color.green;
    [SerializeField] Color inactiveStateColor = Color.yellow;
    [SerializeField] Color stretchedStateColor = Color.red;
    private Transform startTransform;
    private Transform endTransform;
    private Vector3 startLocalPosition;
    private Vector3 endLocalPosition;

    private Vector3 middlePosition;
    private Vector3 startMiddlePosition;
    private Vector3 endMiddlePosition;
    private float bendMult = 1f;
    private float timeToStraighenLine;

    //PRIVATE
    private Vector3[] linePoints = new Vector3[5];
    private Vector3[] linePositions = new Vector3[0];
    private Vector3[] linePositionsOld = new Vector3[0];

    public void Init(Transform startTransform, Vector3 startLocalPosition, Transform endTransform, Vector3 endLocalPosition, bool startsActive, float activationDelay)
    {
        this.startTransform = startTransform;
        this.endTransform = endTransform;
        this.startLocalPosition = startLocalPosition;
        this.endLocalPosition = endLocalPosition;
        timeToStraighenLine = activationDelay;

        if(startsActive) SetLineColorActive();
        else SetLineColorInactive();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        //_lineRenderer.SetPosition(0, startTransform.TransformPoint(startLocalPosition));
        //_lineRenderer.SetPosition(1, endTransform.TransformPoint(endLocalPosition));

        UpdateMiddlePointPosition();
        GetPoints();
        SetPointsToLine();
    }

    public void SetLineColorActive()
    {
        _lineRenderer.startColor = activatedStateColor;
        _lineRenderer.endColor = activatedStateColor;
        StartCoroutine(MakeLineStraight());
    }

    public void SetLineColorInactive()
    {
        _lineRenderer.startColor = inactiveStateColor;
        _lineRenderer.endColor = inactiveStateColor;
    }

    private void UpdateMiddlePointPosition()
    {
        Vector3 startWorldPos = startTransform.TransformPoint(startLocalPosition);
        Vector3 endWorldPos = endTransform.TransformPoint(endLocalPosition);
        Vector3 startToEndVector = endWorldPos - startWorldPos;

        middlePosition = startWorldPos + startToEndVector/2 + Vector3.down * bendAmount * bendMult;
        startMiddlePosition = startWorldPos + startToEndVector / 4 + Vector3.down * bendAmount * 2/3 * bendMult;
        endMiddlePosition = startWorldPos + startToEndVector * 3/4 + Vector3.down * bendAmount * 2 / 3 * bendMult;

        //find curved points in children
        linePoints[0] = startWorldPos;
        linePoints[1] = startMiddlePosition;
        linePoints[2] = middlePosition;
        linePoints[3] = endMiddlePosition;
        linePoints[4] = endWorldPos;
    }

    private void GetPoints()
    {
        Debug.Log("Line");

        //add positions
        linePositions = new Vector3[linePoints.Length];
        for (int i = 0; i < linePoints.Length; i++)
        {
            linePositions[i] = linePoints[i];
        }
    }

    private void SetPointsToLine()
    {
        //create old positions if they dont match
        if (linePositionsOld.Length != linePositions.Length)
        {
            linePositionsOld = new Vector3[linePositions.Length];
        }

        //check if line points have moved
        bool moved = false;
        for (int i = 0; i < linePositions.Length; i++)
        {
            //compare
            if (linePositions[i] != linePositionsOld[i])
            {
                moved = true;
            }
        }

        //update if moved
        if (moved == true)
        {
            LineRenderer line = this.GetComponent<LineRenderer>();

            //get smoothed values
            Vector3[] smoothedPoints = LineSmoother.SmoothLine(linePositions, lineSegmentSize);

            //set line settings
            line.positionCount = smoothedPoints.Length;
            line.SetPositions(smoothedPoints);
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
        }
    }

    IEnumerator MakeLineStraight()
    {
        float timer = timeToStraighenLine;
        while(timer > 0)
        {
            timer -= Time.deltaTime;
            bendMult = Mathf.Pow(timer / timeToStraighenLine, 2);
            yield return null;
        }

        LineRenderer line = this.GetComponent<LineRenderer>();
        line.startWidth = lineWidth * 0.3f;
        line.endWidth = lineWidth * 0.3f;
        lineWidth *= 0.3f;
    }
}
