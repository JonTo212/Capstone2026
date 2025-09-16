using UnityEngine;

public class TetherPull : MonoBehaviour
{
    private float attachThreshold = 0.5f;
    private float pullForce = 50f;
    private bool activated;

    [SerializeField] private Transform startObj;
    [SerializeField] private Transform endObj;

    public Transform StartObj => startObj;
    public Transform EndObj => endObj;

    public bool Activated
    {
        get { return activated; }
        set { activated = value; }
    }

    private void FixedUpdate()
    {
        if (activated)
        {
            if (startObj.GetComponent<Rigidbody>() != null)
            {
                PullObjects(startObj, endObj);
            }
            if(endObj.GetComponent<Rigidbody>() != null)
            {
                PullObjects(endObj, startObj);
            }
        }
    }

    public void SetStartObj(Transform newObj)
    {
        startObj = newObj;
    }

    public void SetEndObj(Transform newObj)
    {
        endObj = newObj;
    }

    private void SetParent(Transform parent, Transform child)
    {
        child.parent = parent;
    }

    private void PullObjects(Transform original, Transform target)
    {
        Rigidbody rb = original.GetComponent<Rigidbody>();
        Vector3 distanceToTarget = target.position - original.position;
        float distance = distanceToTarget.magnitude;

        if(distance > attachThreshold)
        {
            rb.useGravity = false;
        }
        else
        {
            rb.useGravity = false;
        }

        rb.AddForce(distanceToTarget.normalized * pullForce, ForceMode.Force);
    }
}
