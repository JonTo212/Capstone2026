using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TetherPull : MonoBehaviour
{
    [Header("Properties")]
    [SerializeField] private float attachThreshold = 0.5f;
    [SerializeField] private float pullForce = 50f;
    private bool activated;

    [Header("Attachment Components")]
    private Transform startTransform;
    private Transform endTransform;
    private Rigidbody startRb;
    private Rigidbody endRb;
    private Vector3 startAttachLocal;
    private Vector3 endAttachLocal;

    private bool wasStartGravityEnabled;
    private bool wasEndGravityEnabled;

    [Header("Getters")]
    public Transform StartTransform => startTransform;
    public Transform EndTransform => endTransform;
    public Vector3 StartAttachPoint => startRb ? startRb.transform.TransformPoint(startAttachLocal) : startAttachLocal; //if there's a rigidbody, convert to world space
    public Vector3 EndAttachPoint => endRb ? endRb.transform.TransformPoint(endAttachLocal) : endAttachLocal; 

    public bool Activated
    {
        get { return activated; }
        set {  activated = value; }
    }

    private void FixedUpdate()
    {
        if (activated) //this needs to be changed so it doesn't fire every tick
        {
            if (startRb != null)
            {
                PullObject(startRb, EndAttachPoint, wasStartGravityEnabled); //using the getters here because they handle local to world conversion
            }
            if (endRb != null)
            {
                PullObject(endRb, StartAttachPoint, wasEndGravityEnabled);
            }
        }
    }

    public void SetStartPoint(Transform start, Vector3 hitPoint)
    {
        startTransform = start;

        if (start.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            //convert to local space if there's a rigidbody to get relative attachment point
            startAttachLocal = start.InverseTransformPoint(hitPoint);
            startRb = rb;

            if(startRb.useGravity)
            {
                wasStartGravityEnabled = true;
            }
            else
            {
                wasStartGravityEnabled = false;
            }
        }
        else
        {
            //use the world space point otherwise (this means the object is static, so you don't need to save the local conversion as it doesn't rotate)
            startAttachLocal = hitPoint;
        }
    }

    public void SetEndPoint(Transform end, Vector3 hitPoint)
    {
        endTransform = end;

        if (end.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            endAttachLocal = end.InverseTransformPoint(hitPoint);
            endRb = rb;

            if(endRb.useGravity)
            {
                wasEndGravityEnabled = true;
            }
            else
            {
                wasEndGravityEnabled= false;
            }
        }
        else
        {
            endAttachLocal = hitPoint;
        }
    }

    private void PullObject(Rigidbody rb, Vector3 target, bool wasGravityEnabled)
    {
        float force = rb.mass * pullForce;

        //target is in world space
        if (Vector3.Distance(target, rb.position) > attachThreshold || wasGravityEnabled == false)
        {
            rb.useGravity = false;

            //rb.MovePosition(rb.position + ((target - rb.position).normalized * force * Time.fixedDeltaTime));
        }
        else
        {
            rb.useGravity = true;
        }

        rb.AddForce((target - rb.position).normalized * pullForce, ForceMode.Force);

        if(rb.gameObject.GetComponent<Tetherable>() != null )
        {
            rb.gameObject.GetComponent<Tetherable>().forceBeingReceived = (target - rb.position).normalized * pullForce;
        }
    }

    public void ResetTether()
    {
        if(startRb != null && wasStartGravityEnabled)
        {
            startRb.useGravity = true;
        }
        if(endRb != null &&  wasEndGravityEnabled)
        {
            endRb.useGravity = true;
        }

        activated = false;
        startRb = null;
        endRb = null;
        startAttachLocal = Vector3.zero;
        endAttachLocal = Vector3.zero;
    }
}
