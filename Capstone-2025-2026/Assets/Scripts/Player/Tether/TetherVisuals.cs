using UnityEngine;

public class TetherVisuals : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private TetherPull tetherPull;
    private Vector3 startPoint;
    private Vector3 endPoint;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        tetherPull = GetComponent<TetherPull>();
    }

    private void Update()
    {
        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, startPoint);
            lineRenderer.SetPosition(1, endPoint);
        }

        if(tetherPull.Activated)
        {
            SetStartPoint(tetherPull.StartObj.position);
            SetEndPoint(tetherPull.EndObj.position);
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
}
