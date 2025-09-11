using System.Collections;
using UnityEngine;

public class Pull : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Transform holdPos;
    [SerializeField] private LayerMask pullableObjectLayer;
    [SerializeField] private Camera playerCam;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float hitboxRadius;

    [Header("Force Variables")]
    [SerializeField] private float forceMultiplier;
    [SerializeField] private float maxVelocity;
    [SerializeField] private float throwVelocity;

    [Header("Distance Variables")]
    [SerializeField] private float attachThreshold;
    [SerializeField] private float maxPullDistance;

    private Transform heldObj;
    private Coroutine pullCoroutine;
    private PlayerActions playerActions;

    private void Awake()
    {
        playerActions = GetComponent<PlayerActions>();
        playerActions.OnThrowPressed += ThrowHeldObject;
    }

    private void Update()
    {
        //start pull
        if (playerActions.PullInput && pullCoroutine == null && heldObj == null)
            TryStartPull();

        //stop pull
        if (!playerActions.PullInput && pullCoroutine != null)
        {
            if (heldObj != null)
                ReleaseHeldObject();

            lineRenderer.enabled = false;
            StopCoroutine(pullCoroutine);
            pullCoroutine = null;
        }
    }

    private void TryStartPull()
    {
        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.SphereCast(ray, hitboxRadius, out RaycastHit hit, maxPullDistance, pullableObjectLayer))
        {
            pullCoroutine = StartCoroutine(PullObject(hit.transform));
        }
    }

    private void ThrowHeldObject()
    {
        if (heldObj != null)
        {
            Rigidbody rb = heldObj.GetComponent<Rigidbody>();
            heldObj.parent = null;
            rb.constraints = RigidbodyConstraints.None;

            Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            rb.linearVelocity = ray.direction * throwVelocity;

            heldObj = null;
        }
    }

    private void ReleaseHeldObject()
    {
        Rigidbody rb = heldObj.GetComponent<Rigidbody>();
        heldObj.parent = null;
        rb.constraints = RigidbodyConstraints.None;
        heldObj = null;
    }

    private IEnumerator PullObject(Transform targetObj)
    {
        Rigidbody objectRb = targetObj.GetComponent<Rigidbody>();
        lineRenderer.enabled = true;

        while (true)
        {
            Vector3 objToHand = holdPos.position - targetObj.position;
            float distanceToHand = objToHand.magnitude;

            if (distanceToHand < attachThreshold)
            {
                targetObj.position = holdPos.position;
                targetObj.parent = holdPos;
                objectRb.constraints = RigidbodyConstraints.FreezePosition;
                lineRenderer.enabled = false;
                heldObj = targetObj;
                yield break;
            }

            Vector3 pullDir = objToHand.normalized;
            if (objectRb.linearVelocity.magnitude < maxVelocity)
                objectRb.AddForce(pullDir * forceMultiplier, ForceMode.Force);
            else
                objectRb.linearVelocity = pullDir * maxVelocity;

            lineRenderer.SetPosition(0, holdPos.position);
            lineRenderer.SetPosition(1, targetObj.position);

            yield return null;
        }
    }
}
