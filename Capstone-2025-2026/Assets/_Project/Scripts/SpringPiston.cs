using FMODUnity;
using System.Collections;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class SpringPiston : MonoBehaviour
{
    [SerializeField] private Transform basePiece;
    [SerializeField] private Transform compressedPos;
    [SerializeField] private Transform uncompressedPos;
    [SerializeField] private Prop platformPiece;
    [SerializeField] private float decompressionTime = 0.1f;
    [SerializeField] private float compressionTime = 0.6f;
    private Coroutine compressionCoroutine;
    private bool currentlyCompressed;

    private void Update()
    {
        if (IsTetheredInRightDirection() != currentlyCompressed)
        {
            StopCoroutine(compressionCoroutine);
            compressionCoroutine = null;
        }
        currentlyCompressed = IsTetheredInRightDirection();
    }

    private void FixedUpdate()
    {
        if(compressionCoroutine == null)
        {
            if (IsTetheredInRightDirection())
            {
                compressionCoroutine = StartCoroutine(HandleCompression(compressedPos.position, compressionTime));
            }
            else
            {
                compressionCoroutine = StartCoroutine(HandleCompression(uncompressedPos.position, decompressionTime));
            }
            
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

    private  IEnumerator HandleCompression(Vector3 target, float compressionDuration)
    {
        RuntimeManager.PlayOneShot("event:/Piston", transform.position);

        while ((platformPiece.Rb.position - target).sqrMagnitude > 0.0001f)
        {
            Vector3 newPos = Vector3.SmoothDamp(platformPiece.Rb.position, target, ref velocity, compressionDuration);
            platformPiece.Rb.MovePosition(newPos);
            yield return new WaitForFixedUpdate();
        }
        compressionCoroutine = null;
    }
    
}