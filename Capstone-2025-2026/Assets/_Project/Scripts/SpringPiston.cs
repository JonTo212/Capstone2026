using Unity.VisualScripting;
using UnityEngine;

public class SpringPiston : MonoBehaviour
{
    [SerializeField] private Transform basePiece;
    [SerializeField] private Transform compressedPos;
    [SerializeField] private Transform uncompressedPos;
    [SerializeField] private Prop platformPiece;
    [SerializeField] private float decompressionTime = 0.1f;
    [SerializeField] private float compressionTime = 0.6f;

    private void FixedUpdate()
    {
        if(IsTetheredInRightDirection())
        {
            HandleCompression(compressedPos.position, compressionTime);
        }
        else
        {
            HandleCompression(uncompressedPos.position, decompressionTime);
        }
    }

    private bool IsTetheredInRightDirection()
    {
        Vector3 dirToBase = platformPiece.transform.position - basePiece.transform.position;
        if(Vector3.Dot(platformPiece.totalForceApplied, dirToBase) < 0)
        { 
            return true;
        }
        else
        {
            return false;
        }

        /*
        if (platformPiece.ConnectedObjects.Contains(basePiece) || platformPiece.ConnectedObjects.Contains(platformPiece.transform))
            return true;
        else
            return false;
        */
    }

    private Vector3 velocity;

    private void HandleCompression(Vector3 target, float compressionDuration)
    {
        if ((platformPiece.Rb.position - target).sqrMagnitude < 0.0001f) return;

        Vector3 newPos = Vector3.SmoothDamp(platformPiece.Rb.position, target, ref velocity, compressionDuration);
        platformPiece.Rb.MovePosition(newPos);
    }
}