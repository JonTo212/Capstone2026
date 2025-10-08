using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
public class JointTetherCollider : MonoBehaviour
{
    [Header("Properties")]
    private Transform startTransform;
    private Transform endTransform;
    private Vector3 startHitLocalPosition;
    private Vector3 endHitLocalPosition;

    [Header("Components")]
    [SerializeField] private CapsuleCollider capsuleCollider;

    public void Init(Transform startTransform, Vector3 startHitLocalPosition, Transform endTransform, Vector3 endHitLocalPosition)
    {
        this.startTransform = startTransform;
        this.endTransform = endTransform;
        this.startHitLocalPosition = startHitLocalPosition;
        this.endHitLocalPosition = endHitLocalPosition;
    }

    private void Awake()
    {
        capsuleCollider = GetComponent<CapsuleCollider>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    private void FixedUpdate()
    {
        UpdateCollider();
    }

    public void UpdateCollider()
    {
        Vector3 startWorldPos = startTransform.TransformPoint(startHitLocalPosition);
        Vector3 endWorldPos = endTransform.TransformPoint(endHitLocalPosition);

        capsuleCollider.height = Vector3.Distance(startWorldPos, endWorldPos);

        Vector3 startEndVector = endWorldPos - startWorldPos;
        Vector3 startEndNormal = Vector3.Cross(startEndVector, new Vector3(0, 1, 0));

        transform.rotation = Quaternion.LookRotation(startEndNormal, startEndVector);
    }
}
