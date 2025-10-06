using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class JointTetherVisuals : MonoBehaviour
{
    private LineRenderer _lineRenderer;

    [Header("Line Variables")]
    [SerializeField] Color activatedStateColor = Color.green;
    [SerializeField] Color inactiveStateColor = Color.yellow;
    [SerializeField] Color stretchedStateColor = Color.red;
    private Transform startTransform;
    private Transform endTransform;
    private Vector3 startLocalPosition;
    private Vector3 endLocalPosition;

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
}
