using System;
using System.Collections;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.Profiling;
using static UnityEngine.GraphicsBuffer;

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

    [Header("Private Variables")]
    private Transform heldObj;
    private Vector3 targetPos;
    private Coroutine pullCoroutine;
    private PlayerActions playerActions;

    /* Projectile Components
    private bool projectileActive = false;
    private Vector3 projectilePosition;
    private Vector3 projectileVelocity;
    [SerializeField] private float projectileSpeed = 50f;
    private bool fired;
    */

    [Header("Getters")]
    public Coroutine PullCoroutine => pullCoroutine;
    public Transform HoldPos => holdPos; 
    public Vector3 TargetPos => targetPos;

    #region Unity Functions
    private void Awake()
    {
        playerActions = GetComponent<PlayerActions>();
        playerActions.OnThrowPressed += ThrowHeldObject;
        targetPos = holdPos.position;
    }

    private void Update()
    {
        if (playerActions.PullInput)
        {
            if (heldObj == null && pullCoroutine == null)
            {
                TryStartPull();
            }
        }

        if (!playerActions.PullInput)
        {
            if (pullCoroutine != null)
            {
                StopCoroutine(pullCoroutine);
                pullCoroutine = null;
            }

            if (heldObj != null)
            {
                ReleaseHeldObject();
            }
        }
    }
    #endregion

    /*#region Projectile cast (UNUSED)

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
            fired = true;
        }
    }

    #endregion*/

    #region Pull
    private void TryStartPull()
    {
        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.SphereCast(ray, hitboxRadius, out RaycastHit hit, maxPullDistance, pullableObjectLayer))
        {
            targetPos = hit.transform.position;
            pullCoroutine = StartCoroutine(PullObject(hit.transform));
        }
        else
        {
            targetPos = ray.origin + ray.direction * maxPullDistance;
        }
    }

    private IEnumerator PullObject(Transform target)
    {
        Rigidbody objectRb = target.GetComponent<Rigidbody>();

        while (Vector3.Distance(holdPos.position, target.position) > attachThreshold)
        {
            Vector3 objToHand = holdPos.position - target.position;
            Vector3 pullDir = objToHand.normalized;

            if (objectRb.linearVelocity.magnitude < maxVelocity)
                objectRb.AddForce(pullDir * forceMultiplier, ForceMode.Force);
            else
                objectRb.linearVelocity = pullDir * maxVelocity;

            targetPos = target.position;

            yield return null;
        }

        HoldObject(target);
    }
    #endregion

    #region Throwing / Holding
    private void ThrowHeldObject()
    {
        if (heldObj != null)
        {
            Rigidbody rb = heldObj.GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.None;

            Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            rb.linearVelocity = ray.direction * throwVelocity;
            
            heldObj.parent = null;
            heldObj = null;
            targetPos = holdPos.position;
        }
    }

    private void HoldObject(Transform target)
    {
        target.position = holdPos.position;
        target.parent = holdPos;
        target.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePosition;
        targetPos = holdPos.position;
        heldObj = target;
    }

    private void ReleaseHeldObject()
    {
        heldObj.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
        heldObj.parent = null;
        heldObj = null;
        targetPos = holdPos.position;
    }
    #endregion
}