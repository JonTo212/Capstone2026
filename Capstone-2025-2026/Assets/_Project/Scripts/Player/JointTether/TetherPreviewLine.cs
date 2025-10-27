using Unity.VisualScripting;
using UnityEngine;

public class TetherPreviewLine : MonoBehaviour
{
    [Header("Components")]
    private LineRenderer lineRenderer;

    [Header("Variables")]
    [SerializeField] private Color previewColor = Color.yellow;
    [SerializeField] private Color blockedColor = Color.red;
    [SerializeField] private LayerMask tetherLayerMask;
    private Vector3 startPoint;
    private Vector3 endPoint;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        CheckIfSomethingIsBlocking();
    }

    public void SetStartPoint(Vector3 newPos)
    {
        startPoint = newPos;
        lineRenderer.SetPosition(0, newPos);
    }

    public void SetEndPoint(Vector3 newPos)
    {
        endPoint = newPos;
        lineRenderer.SetPosition(1, newPos);
    }

    private void CheckIfSomethingIsBlocking()
    {
        RaycastHit hit;
        
        Vector3 startEndDirection = (endPoint - startPoint).normalized;
        float startEndDistance = (endPoint - startPoint).magnitude;

        if(Physics.Raycast(startPoint, startEndDirection,out hit, startEndDistance, ~tetherLayerMask, QueryTriggerInteraction.Ignore))
        {
            lineRenderer.startColor = blockedColor;
            lineRenderer.endColor = blockedColor;
        }
        else
        {
            lineRenderer.startColor = previewColor;
            lineRenderer.endColor = previewColor;
        }
    }
}
