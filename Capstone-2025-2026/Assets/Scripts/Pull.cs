using System;
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

    private bool projectileActive = false;
    private Vector3 projectilePosition;
    private Vector3 projectileVelocity;
    [SerializeField] private float projectileSpeed = 50f;
    private bool fired;

    #region Unity Functions
    private void Awake()
    {
        playerActions = GetComponent<PlayerActions>();
        playerActions.OnThrowPressed += ThrowHeldObject;
    }

    private void Update()
    {
        if (playerActions.PullInput)
        {
            if (!projectileActive && heldObj == null && !fired)
                HandleProjectileStart();

            else if(projectileActive)
                SimulateProjectile();
        }

        if (!playerActions.PullInput)
        {
            if (pullCoroutine != null)
            {
                StopCoroutine(pullCoroutine);
                pullCoroutine = null;
            }

            if (heldObj != null)
                ReleaseHeldObject();

            projectileActive = false;
            lineRenderer.enabled = false;
            fired = false;
        }
    }
    #endregion

    #region Projectile cast

    private void HandleProjectileStart()
    {
        projectilePosition = playerCam.transform.position;
        projectileVelocity = playerCam.transform.forward * projectileSpeed;
        projectileActive = true;
    }

    private void SimulateProjectile()
    {
        //simulate projectile movement
        projectilePosition += projectileVelocity * Time.deltaTime;

        //spherecast along projectile path, if it hits something begin pull
        if (Physics.SphereCast(projectilePosition, hitboxRadius, projectileVelocity.normalized, out RaycastHit hit, projectileVelocity.magnitude * Time.deltaTime, pullableObjectLayer))
        {
            projectileActive = false;
            pullCoroutine = StartCoroutine(PullObject(hit.transform));
            fired = true;
        }

        //stop once you reach max range
        if (Vector3.Distance(playerCam.transform.position, projectilePosition) > maxPullDistance)
        {
            projectileActive = false;
            lineRenderer.enabled = false;
            fired = true;
        }

        // Optional: update a line renderer or projectile visual
        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, holdPos.position);
        lineRenderer.SetPosition(1, projectilePosition);
    }

    #endregion

    #region Pull
    private void TryStartPull()
    {
        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.SphereCast(ray, hitboxRadius, out RaycastHit hit, maxPullDistance, pullableObjectLayer))
        {
            pullCoroutine = StartCoroutine(PullObject(hit.transform));
        }
    }
    private IEnumerator PullObject(Transform targetObj)
    {
        Rigidbody objectRb = targetObj.GetComponent<Rigidbody>();
        lineRenderer.enabled = true;

        while (Vector3.Distance(holdPos.position, targetObj.position) > attachThreshold)
        {
            Vector3 objToHand = holdPos.position - targetObj.position;
            Vector3 pullDir = objToHand.normalized;

            if (objectRb.linearVelocity.magnitude < maxVelocity)
                objectRb.AddForce(pullDir * forceMultiplier, ForceMode.Force);
            else
                objectRb.linearVelocity = pullDir * maxVelocity;

            lineRenderer.SetPosition(0, holdPos.position);
            lineRenderer.SetPosition(1, targetObj.position);

            yield return null;
        }

        HoldObject(targetObj, objectRb);
    }
    #endregion

    #region Throwing / Holding
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

    private void HoldObject(Transform targetObj, Rigidbody objectRb)
    {
        targetObj.position = holdPos.position;
        targetObj.parent = holdPos;
        objectRb.constraints = RigidbodyConstraints.FreezePosition;
        lineRenderer.enabled = false;
        heldObj = targetObj;
    }

    private void ReleaseHeldObject()
    {
        Rigidbody rb = heldObj.GetComponent<Rigidbody>();
        heldObj.parent = null;
        rb.constraints = RigidbodyConstraints.None;
        heldObj = null;
    }
    #endregion
}
