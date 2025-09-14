using UnityEngine;

public enum TetherState
{
    Empty,
    Firing,
    Anchored,
    Pulling,
    Held
}

public class Tether : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Transform holdPos;
    [SerializeField] private LayerMask pullableObjectLayer;
    [SerializeField] private Camera playerCam;
    [SerializeField] private float hitboxRadius;

    [Header("Force Variables")]
    [SerializeField] private float tetherForceMultiplier;
    [SerializeField] private float pullForceMultiplier;
    [SerializeField] private float maxVelocity;
    [SerializeField] private float throwVelocity;
    [SerializeField] private AnimationCurve pullVelocityCurve;

    [Header("Distance Variables")]
    [SerializeField] private float attachThreshold;
    [SerializeField] private float maxPullDistance;

    [Header("Private Variables")]
    private PlayerActions playerActions;
    private TetherState currentTetherState;

    [Header("Projectile Components")]
    [SerializeField] private float projectileSpeed = 50f;
    private Vector3 projectilePosition;
    private Vector3 projectileVelocity;

    [Header("Tether Components")]
    [SerializeField] private Transform anchoredPos;
    private Transform tetheredObj;
    private Rigidbody tetheredBody;

    [Header("Getters")]
    public Transform HoldPos => holdPos; 
    public Transform TetheredObj => tetheredObj;
    public Transform AnchoredPos => anchoredPos;
    public TetherState CurrentTetherState => currentTetherState;

    #region Unity Functions
    private void Awake()
    {
        playerActions = GetComponent<PlayerActions>();
        SwitchTetherState(TetherState.Empty);
        pullVelocityCurve = new AnimationCurve(
            new Keyframe(0f, 0f),          // start (no pull)
            new Keyframe(0.3f, 1.5f),      // overshoot above target
            new Keyframe(0.6f, 0.75f),      // dip below
            new Keyframe(0.8f, 1.25f),     // smaller overshoot
            new Keyframe(1f, 1f)           // settle exactly at target
        );
    }

    private void Update()
    {
        HandleTetherState();
    }
    #endregion

    #region State Handler

    private void HandleTetherState()
    {
        switch(currentTetherState)
        {
            case TetherState.Empty:
                HandleEmptyState();
                break;

            case TetherState.Firing:
                HandleFiringState();
                break;

            case TetherState.Anchored:
                HandleAnchoredState();
                break;

            case TetherState.Pulling:
                HandlePullState();
                break;

            case TetherState.Held:
                HandleHeldState();
                break;

        }
    }

    private void SwitchTetherState(TetherState newState)
    {
        currentTetherState = newState;
    }

    private void HandleEmptyState()
    {
        //if left click, start shooting projectile
        if (playerActions.PullDown)
        {
            HandleProjectileStart();
        }
    }

    private void HandleFiringState()
    {
        //if within max firing distance, move projectile, otherwise reset
        if (Vector3.Distance(holdPos.position, projectilePosition) < maxPullDistance)
        {
            SimulateProjectile();
        }
        else
        {
            ResetAnchor();
            SwitchTetherState(TetherState.Empty);
        }
    }

    private void HandleAnchoredState()
    {
        //move to center of screen and snap when close enough
        if (Vector3.Distance(anchoredPos.position, tetheredObj.position) > attachThreshold)
        {
            PullObject(anchoredPos);
        }
        else
        {
            HoldObject(anchoredPos);
        }

        //if left click again, release
        if(playerActions.PullDown)
        {
            ReleaseHeldObject();
            ResetAnchor();
            SwitchTetherState(TetherState.Empty);
        }

        //if right click, start pull
        if(playerActions.ThrowDown)
        {
            SwitchTetherState(TetherState.Pulling);
        }
    }

    private void HandlePullState()
    {
        //if you're holding right click, pull object towards you and snap when close enough
        if (playerActions.ThrowHeld)
        {
            tetheredBody.constraints = RigidbodyConstraints.None;
            float currentDistance = Vector3.Distance(anchoredPos.position, holdPos.position);
            float normalizedProgress = 1f - Mathf.Clamp01(currentDistance / maxPullDistance);
            float curveFactor = pullVelocityCurve.Evaluate(normalizedProgress);
            float pullForce = curveFactor * pullForceMultiplier;

            if (Vector3.Distance(holdPos.position, tetheredObj.position) > attachThreshold)
            {
                Vector3 newPos = Vector3.Lerp(anchoredPos.position, holdPos.position, pullForce * Time.deltaTime);
                anchoredPos.position = newPos;
            }
            else
            {
                HoldObject(holdPos);
                SwitchTetherState(TetherState.Held);
            }
        }

        //otherwise go back to anchor
        else if(playerActions.ThrowUp)
        {
            SwitchTetherState(TetherState.Anchored);
        }    
    }

    private void HandleHeldState()
    {
        //if you're holding the object, throw it away
        if (playerActions.ThrowDown)
        {
            ThrowHeldObject();
            ResetAnchor();
            SwitchTetherState(TetherState.Empty);
        }
        
        if(playerActions.PullDown)
        {
            ReleaseHeldObject();
            ResetAnchor();
            SwitchTetherState(TetherState.Empty);
        }
    }

    #endregion

    #region Projectile cast

    private void HandleProjectileStart()
    {
        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        projectilePosition = ray.origin;
        projectileVelocity = ray.direction * projectileSpeed;

        SwitchTetherState(TetherState.Firing);
    }

    private void SimulateProjectile()
    {
        //simulate projectile movement
        projectilePosition += projectileVelocity * Time.deltaTime;
        anchoredPos.position = projectilePosition;

        //spherecast along projectile path, if it hits something begin pull
        if (Physics.SphereCast(projectilePosition, hitboxRadius, projectileVelocity.normalized, out RaycastHit hit, projectileVelocity.magnitude * Time.deltaTime, pullableObjectLayer))
        {
            tetheredObj = hit.transform;
            tetheredBody = hit.transform.GetComponent<Rigidbody>();
            SwitchTetherState(TetherState.Anchored);
        }
    }

    #endregion

    #region Pull

    private void PullObject(Transform target)
    {
        Vector3 objToHand = target.position - tetheredObj.position;
        Vector3 pullDir = objToHand.normalized;

        if (tetheredBody.linearVelocity.magnitude < maxVelocity)
            tetheredBody.AddForce(pullDir * tetherForceMultiplier, ForceMode.Force);
        else
            tetheredBody.linearVelocity = pullDir * maxVelocity;
    }
    #endregion

    #region Throwing / Holding
    private void ThrowHeldObject()
    {
        if (tetheredObj != null)
        {
            tetheredBody.constraints = RigidbodyConstraints.None;

            Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            tetheredBody.linearVelocity = ray.direction * throwVelocity;

            tetheredBody.useGravity = true;
            tetheredBody = null;
            tetheredObj.parent = null;
            tetheredObj = null;
        }
    }

    private void HoldObject(Transform targetPos)
    {
        tetheredObj.position = targetPos.position;
        tetheredObj.parent = targetPos;
        tetheredBody.MovePosition(targetPos.position);
        tetheredBody.constraints = RigidbodyConstraints.FreezePosition;
        tetheredBody.useGravity = false;
    }

    private void ReleaseHeldObject()
    {
        tetheredBody.constraints = RigidbodyConstraints.None;
        tetheredBody.useGravity = true;
        tetheredBody = null;
        tetheredObj.parent = null;
        tetheredObj = null;
    }

    private void ResetAnchor()
    {
        projectilePosition = holdPos.position;
        anchoredPos.position = holdPos.position;
    }
    #endregion
}