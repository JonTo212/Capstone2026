using System.Collections;
using Unity.Cinemachine;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class JointTetherVisuals : MonoBehaviour
{
    private LineRenderer _lineRenderer;

    [Header("Line Variables")]
    [SerializeField] private float lineSegmentSize = 0.15f;
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private float bendAmount = 0.5f;
    [SerializeField] Color stretchedStateColor = Color.red;

    [Header("Activated Colors")]
    [SerializeField] Color activatedStateColor = Color.green;
    [ColorUsage(true,true)] [SerializeField] Color activatedEmmissive = Color.green;
    [SerializeField] Color activatedHiddenStateColor = Color.green;
    [ColorUsage(true, true)][SerializeField] Color activatedHiddenEmmissive = Color.green;

    [Header("Inactive Colors")]
    [SerializeField] Color inactiveStateColor = Color.yellow;
    [ColorUsage(true, true)][SerializeField] Color inactiveEmmissive = Color.yellow;
    [SerializeField] Color inactiveHiddenStateColor = Color.yellow;
    [ColorUsage(true, true)][SerializeField] Color inactiveHiddenEmmissive = Color.yellow;

    [Header("Selected Colors")]
    [SerializeField] Color selectedStateColor = Color.yellow;
    [ColorUsage(true, true)][SerializeField] Color selectedEmmissive = Color.yellow;
    [SerializeField] Color selectedHiddenStateColor = Color.yellow;
    [ColorUsage(true, true)][SerializeField] Color selectedHiddenEmmissive = Color.yellow;

    [SerializeField] Material tetherLineMaterial;
    private Material[] tetherLineMatInstances;
    //[SerializeField] Material attachedMaterial;
    //[SerializeField] Material activatedMaterial;
    //[SerializeField] Material selectedMaterial;

    [Header("Attachment Point Variables")]
    [SerializeField] private Transform startPointVisuals;
    [SerializeField] private Transform endPointVisuals;
    [SerializeField] private Material inactivePoint;
    [SerializeField] private Material activatedPoint;
    [SerializeField] private Material inactiveRing;
    [SerializeField] private Material activatedRing;

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

        GetMaterialInstances();

        if (startsActive) SetLineColorActive();
        else SetLineColorInactive();

    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
    }

    private void Start()
    {
        startPointVisuals.parent = null;
        endPointVisuals.parent = null;

        startPointVisuals.eulerAngles = Vector3.zero;
        endPointVisuals.eulerAngles = Vector3.zero;

        startPointVisuals.localScale = Vector3.one;
        endPointVisuals.localScale = Vector3.one;
    }

    // Update is called once per frame
    void Update()
    {
        //_lineRenderer.SetPosition(0, startTransform.TransformPoint(startLocalPosition));
        //_lineRenderer.SetPosition(1, endTransform.TransformPoint(endLocalPosition));
        if (startTransform == null || endTransform == null) return;
        UpdateMiddlePointPosition();
        GetPoints();
        SetPointsToLine();
        UpdateAttachmentPointVisualPosition();
    }

    public void SetLineColorActive()
    {
        foreach (Material mat in tetherLineMatInstances)
        {
            mat.SetColor("_Color", activatedStateColor);
            mat.SetColor("_Emissive", activatedEmmissive);
            mat.SetColor("_HiddenEmissive", activatedHiddenEmmissive);
        }

        //_lineRenderer.material = activatedMaterial;
        //StartCoroutine(MakeLineStraight());
    }

    public void SetLineColorInactive()
    {
        foreach (Material mat in tetherLineMatInstances)
        {
            mat.SetColor("_Color", inactiveHiddenStateColor);
            mat.SetColor("_Emissive", inactiveEmmissive);
            mat.SetColor("_HiddenEmissive", inactiveHiddenEmmissive);
        }
    }

    public void SetLineColorSelected()
    {
        foreach (Material mat in tetherLineMatInstances)
        {
            mat.SetColor("_Color", selectedStateColor);
            mat.SetColor("_Emissive", selectedEmmissive);
            mat.SetColor("_HiddenEmissive", selectedHiddenEmmissive);
        }
    }

    private void GetMaterialInstances()
    {
        tetherLineMatInstances = new Material[5];
        tetherLineMatInstances[0] = GetComponent<LineRenderer>().material;
        tetherLineMatInstances[1] = startPointVisuals.GetChild(0).GetComponent<MeshRenderer>().material;
        tetherLineMatInstances[2] = startPointVisuals.GetChild(1).GetComponent<MeshRenderer>().material;
        tetherLineMatInstances[3] = endPointVisuals.GetChild(0).GetComponent<MeshRenderer>().material;
        tetherLineMatInstances[4] = endPointVisuals.GetChild(1).GetComponent<MeshRenderer>().material;
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

    private void UpdateAttachmentPointVisualPosition()
    {
        Vector3 startWorldPos = startTransform.TransformPoint(startLocalPosition);
        Vector3 endWorldPos = endTransform.TransformPoint(endLocalPosition);

        startPointVisuals.position = startWorldPos;
        endPointVisuals.position = endWorldPos;
    }

    private void GetPoints()
    {
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
        line.startWidth = lineWidth * 0.4f;
        line.endWidth = lineWidth * 0.4f;
        lineWidth *= 0.4f;
    }

    private void OnDestroy()
    {
        if(startPointVisuals != null)
        {
            Destroy(startPointVisuals.gameObject);
        }
        if(endPointVisuals != null)
        {
            Destroy(endPointVisuals.gameObject);
        }
    }
}
