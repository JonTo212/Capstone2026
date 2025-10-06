using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class JointTetherVisuals : MonoBehaviour
{
    private LineRenderer _lineRenderer;

    [Header("Line Variables")]
    [SerializeField] Color regularStateColor = Color.green;
    [SerializeField] Color stretchedStateColor = Color.red;
    private Transform startTransform;
    private Transform endTransform;
    private Vector3 startLocalPosition;
    private Vector3 endLocalPosition;

    public void Init(Transform startTransform, Vector3 startLocalPosition, Transform endTransform, Vector3 endLocalPosition)
    {
        this.startTransform = startTransform;
        this.endTransform = endTransform;
        this.startLocalPosition = startLocalPosition;
        this.endLocalPosition = endLocalPosition;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        _lineRenderer.SetPosition(0, startTransform.TransformPoint(startLocalPosition));
        _lineRenderer.SetPosition(1, endTransform.TransformPoint(endLocalPosition));

        _lineRenderer.startColor = regularStateColor;
        _lineRenderer.endColor = regularStateColor;
    }
}
