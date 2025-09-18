using System.Collections;
using UnityEngine;

public class UpdatedLasso : MonoBehaviour
{
    [Header("External Components")]
    [SerializeField] private Camera playerCam;
    [SerializeField] private Tetherable snaredObj;
    [SerializeField] private Transform holdPos;

    [Header("Lasso Properties")]
    [SerializeField] private float lassoRange;
    [SerializeField] private float lassoPullStrength;
    [SerializeField] private float breakDist;
    [SerializeField] private float attachThreshold;

    [Header("Lasso Forces")]
    [SerializeField] private float centerStrength;
    [SerializeField] private float yankStrength;
    [SerializeField] private float throwStrength;

    [Header("Internal Variables")]
    private float anchorDist;
    private Coroutine yankCoroutine;

    [Header("Projectile Properties")]
    [SerializeField] private float hitboxRadius;
    [SerializeField] private float projectileSpeed;
    private Vector3 projectilePosition;
    private Vector3 projectileVelocity;
    private Coroutine projectileCoroutine;

    [Header("Getters")]
    public bool HasSnaredObject => snaredObj != null;
    public Tetherable SnaredObject => snaredObj;
    public Transform HoldPos => holdPos;
    public float AnchorDistance => anchorDist;
    public Coroutine YankCoroutine => yankCoroutine;
    public Coroutine ProjectileCoroutine => projectileCoroutine;
    public Vector3 ProjectilePosition => projectilePosition;

    #region Helper Functions

    public Vector3 GetCenterOfScreen()
    {
        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 maxDistancePos = ray.origin + ray.direction * anchorDist;
        return maxDistancePos;
    }

    #endregion

    #region Start Lasso
    public void TryLasso()
    {
        if (projectileCoroutine != null)
            StopCoroutine(projectileCoroutine);

        projectileCoroutine = StartCoroutine(SimulateProjectile());
    }

    private IEnumerator SimulateProjectile()
    {
        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        projectilePosition = ray.origin;
        projectileVelocity = ray.direction * projectileSpeed;

        while (Vector3.Distance(holdPos.position, projectilePosition) < lassoRange)
        {
            projectilePosition += projectileVelocity * Time.deltaTime;

            if (Physics.SphereCast(projectilePosition, hitboxRadius, projectileVelocity.normalized, out RaycastHit hit, projectileVelocity.magnitude))
            {
                if (hit.transform.TryGetComponent(out Tetherable tetherable))
                {
                    anchorDist = Vector3.Distance(hit.point, holdPos.position);

                    tetherable.OnPickUp();
                    tetherable.SetLinearDamping(25f);
                    snaredObj = tetherable;
                }

                projectileCoroutine = null;
                yield break; //stop coroutine on hit
            }

            yield return null;
        }

        projectileCoroutine = null;
    }

    public void UpdateAnchorDistance(float scrollWheelDirection)
    {
        anchorDist += (scrollWheelDirection * lassoPullStrength);
    }

    #endregion

    #region Center Lasso (continuous)
    public void MoveObjectToPos(Vector3 desiredPos)
    {
        if(snaredObj != null)
        {
            Vector3 dirToHoldPos = desiredPos - snaredObj.transform.position;
            if (dirToHoldPos.magnitude < attachThreshold) return;

            float distance = dirToHoldPos.magnitude;

            if (distance > breakDist)
            {
                snaredObj.OnRelease();
                snaredObj = null;
                return;
            }

            snaredObj.ApplyForceInDirection(dirToHoldPos.normalized, centerStrength * distance, ForceMode.Force);
        }
    }
    #endregion

    #region Yank
    public void YankObject()
    {
        if (snaredObj != null)
        {
            if(yankCoroutine != null)
                StopCoroutine(yankCoroutine);

            yankCoroutine = StartCoroutine(Yank());
        }
    }

    private IEnumerator Yank()
    {
        while(Vector3.Distance(snaredObj.transform.position, holdPos.position) > attachThreshold)
        {
            Vector3 dirToHoldPos = holdPos.position - snaredObj.transform.position;
            snaredObj.ApplyForceInDirection(dirToHoldPos.normalized, yankStrength, ForceMode.Force);

            yield return null;
        }

        yankCoroutine = null;
    }

    public void PullObject(float scrollDirection)
    {
        if (snaredObj != null)
        {
            Vector3 dirToHoldPos = (holdPos.position - snaredObj.transform.position) * scrollDirection;
            snaredObj.ApplyForceInDirection(dirToHoldPos.normalized, yankStrength, ForceMode.Impulse);
        }
    }

    #endregion

    #region Throw / Release
    public void ThrowObject()
    {
        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        snaredObj.ApplyForceInDirection(ray.direction, throwStrength, ForceMode.Impulse);
        ReleaseObject();
    }

    public void ReleaseObject()
    {
        if(snaredObj != null)
        {
            snaredObj.OnRelease();
            snaredObj = null;
        }
    }
    #endregion
}
