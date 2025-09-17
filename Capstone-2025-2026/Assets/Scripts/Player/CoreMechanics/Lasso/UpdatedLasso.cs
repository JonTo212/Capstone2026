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
    [SerializeField] private float breakDist;
    [SerializeField] private float attachThreshold;

    [Header("Lasso Forces")]
    [SerializeField] private float centerStrength;
    [SerializeField] private float pullStrength;
    [SerializeField] private float throwStrength;

    [Header("Internal Variables")]
    private float anchorDist;
    private float anchorMagnitude;
    private Coroutine yankCoroutine;

    [Header("Getters")]
    public bool HasSnaredObject => snaredObj != null;
    public Tetherable SnaredObject => snaredObj;
    public Transform HoldPos => holdPos;
    public float AnchorDistance => anchorDist;
    public Coroutine YankCoroutine => yankCoroutine;

    #region Helper Functions

    public Vector3 GetCenterOfScreen()
    {
        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 maxDistancePos = ray.origin + ray.direction * anchorDist;
        Vector3 minDistancePos = holdPos.position;
        return Vector3.Lerp(minDistancePos, maxDistancePos, anchorMagnitude);
    }

    #endregion

    #region Start Lasso
    public void TryLasso()
    {
        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.SphereCast(ray, 0.2f, out RaycastHit hit, lassoRange))
        {
            anchorDist = Vector3.Distance(hit.point, holdPos.position);
            anchorMagnitude = 1f;

            if (hit.transform.TryGetComponent(out Tetherable tetherable))
            {
                tetherable.OnPickUp();
                tetherable.SetLinearDamping(25f);
                snaredObj = tetherable;
            }
        }
    }
    #endregion

    #region Center Lasso (continuous)
    public void MoveObjectToLassoPos(Vector3 desiredPos)
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

            snaredObj.ApplyForceInDirection(dirToHoldPos.normalized, centerStrength, ForceMode.Acceleration);
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
            snaredObj.ApplyForceInDirection(dirToHoldPos.normalized, pullStrength, ForceMode.Force);

            yield return null;
        }

        yankCoroutine = null;
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
