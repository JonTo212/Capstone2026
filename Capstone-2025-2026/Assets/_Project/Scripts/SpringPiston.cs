using Unity.VisualScripting;
using UnityEngine;

public enum Axis
{
    X,
    Y,
    Z   
}

public class SpringPiston : MonoBehaviour
{
    [SerializeField] private Axis movementAxis;
    [SerializeField] private Transform basePiece;
    [SerializeField] private Prop platformPiece;
    [SerializeField] private float extensionLength = 3f;
    [SerializeField] private float decompressionTime = 0.1f;
    [SerializeField] private float compressionTime = 0.6f;

    private Vector3 targetPos;
    private Vector3 basePos;
    private float headHalfLength;

    private void OnValidate()
    {
        if (platformPiece == null || !platformPiece.TryGetComponent(out Collider col))
            return;

        GetPistonHeadSize();
        SetUpMaxPos();
    }

    private void FixedUpdate()
    {
        if(IsTetheredToBase())
        {
            HandleCompression(basePos, compressionTime);
        }
        else
        {
            HandleCompression(targetPos, decompressionTime);
        }
    }

    private bool IsTetheredToBase()
    {
        if(platformPiece.ConnectedObjects.Contains(basePiece) || platformPiece.ConnectedObjects.Contains(platformPiece.transform)) 
            return true;
        else
            return false;
    }

    private void GetPistonHeadSize()
    {
        Collider col = platformPiece.GetComponent<Collider>();

        Vector3 axisDir = movementAxis switch
        {
            Axis.X => transform.right,
            Axis.Y => transform.up,
            Axis.Z => transform.forward,
            _ => transform.up
        };

        Bounds b = col.bounds;
        Vector3 extents = b.extents;

        headHalfLength =
            Mathf.Abs(Vector3.Dot(axisDir, Vector3.right)) * extents.x +
            Mathf.Abs(Vector3.Dot(axisDir, Vector3.up)) * extents.y +
            Mathf.Abs(Vector3.Dot(axisDir, Vector3.forward)) * extents.z;
    }

    private void SetUpMaxPos()
    {
        Vector3 axis = movementAxis switch
        {
            Axis.X => transform.right,
            Axis.Y => transform.up,
            Axis.Z => transform.forward,
            _ => transform.up
        };

        basePos = basePiece.position + axis * headHalfLength * 2;
        targetPos = basePos + axis * (extensionLength - headHalfLength * 2);
    }

    private Vector3 velocity;

    private void HandleCompression(Vector3 target, float compressionDuration)
    {
        if ((platformPiece.Rb.position - target).sqrMagnitude < 0.0001f) return;

        Vector3 newPos = Vector3.SmoothDamp(platformPiece.Rb.position, target, ref velocity, compressionDuration);
        platformPiece.Rb.MovePosition(newPos);
    }
}