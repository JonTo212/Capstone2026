using UnityEngine;

public class TetherVisuals : MonoBehaviour
{
    [Header("Components")]
    private LineRenderer lineRenderer;
    private TetherPull tetherPull;

    [Header("Variables")]
    private Vector3 startPoint;
    private Vector3 endPoint;
    private Vector3 lastStartPos;
    private Vector3 lastEndPos;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        tetherPull = GetComponent<TetherPull>();

        lastStartPos = Vector3.zero;
        lastEndPos = Vector3.zero;
    }

    private void Update()
    {
        if (tetherPull.Activated)
        {
            startPoint = tetherPull.StartAttachPoint;
            endPoint = tetherPull.EndAttachPoint;
        }

        if (startPoint != lastStartPos || endPoint != lastEndPos)
        {
            lineRenderer.SetPosition(0, startPoint);
            lineRenderer.SetPosition(1, endPoint);

            lastStartPos = startPoint;
            lastEndPos = endPoint;
        }
    }

    public void SetStartPoint(Vector3 newPos)
    {
        startPoint = newPos;
    }

    public void SetEndPoint(Vector3 newPos)
    {
        endPoint = newPos;
    }

    public void PreviewPull()
    {
        lineRenderer.startColor = Color.yellow;
        lineRenderer.endColor = Color.yellow;
    }

    public void ActivatePull()
    {
        lineRenderer.startColor = Color.green;
        lineRenderer.endColor = Color.green;
    }

    public void ResetPull()
    {
        lineRenderer.positionCount = 0;
        startPoint = Vector3.zero;
        endPoint = Vector3.zero;
        lastStartPos = Vector3.zero;
        lastEndPos = Vector3.zero;
    }
}
