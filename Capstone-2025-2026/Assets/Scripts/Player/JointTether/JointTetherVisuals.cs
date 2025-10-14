using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class JointTetherVisuals : MonoBehaviour
{
    private LineRenderer _lineRenderer;

    [Header("Line Variables")]
    public float lineSegmentSize = 0.15f;
    public float lineWidth = 0.1f;
    [SerializeField] Color activatedStateColor = Color.green;
    [SerializeField] Color inactiveStateColor = Color.yellow;
    [SerializeField] Color stretchedStateColor = Color.red;
    private Transform startTransform;
    private Transform endTransform;
    private Vector3 startLocalPosition;
    private Vector3 endLocalPosition;

    private Vector3 middlePosition;
    private Vector3 startMiddelPosition;
    private Vector3 endMiddelPosition;

    //PRIVATE
    private CurvedLinePoint[] linePoints = new CurvedLinePoint[0];
    private Vector3[] linePositions = new Vector3[0];
    private Vector3[] linePositionsOld = new Vector3[0];

    public void Init(Transform startTransform, Vector3 startLocalPosition, Transform endTransform, Vector3 endLocalPosition, bool startsActive)
    {
        this.startTransform = startTransform;
        this.endTransform = endTransform;
        this.startLocalPosition = startLocalPosition;
        this.endLocalPosition = endLocalPosition;

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
        _lineRenderer.SetPosition(0, startTransform.TransformPoint(startLocalPosition));
        _lineRenderer.SetPosition(1, endTransform.TransformPoint(endLocalPosition));
    }

    public void SetLineColorActive()
    {
        _lineRenderer.startColor = activatedStateColor;
        _lineRenderer.endColor = activatedStateColor;
    }

    public void SetLineColorInactive()
    {
        _lineRenderer.startColor = inactiveStateColor;
        _lineRenderer.endColor = inactiveStateColor;
    }

    void GetPoints()
    {
        Debug.Log("Line");

        //find curved points in children
        linePoints = this.GetComponentsInChildren<CurvedLinePoint>();

        //add positions
        linePositions = new Vector3[linePoints.Length];
        for (int i = 0; i < linePoints.Length; i++)
        {
            linePositions[i] = linePoints[i].transform.position;
        }
    }

    void SetPointsToLine()
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
}
