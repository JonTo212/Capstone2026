using UnityEngine;

public enum PlayerLassoState
{
    Empty,
    Firing,
    Anchored,
    Pulling,
    Held
}

public class PlayerLasso : MonoBehaviour
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
    private PlayerLassoState currentLassoState;

    [Header("Projectile Components")]
    [SerializeField] private float projectileSpeed = 50f;
    private Vector3 projectilePosition;
    private Vector3 projectileVelocity;

    [Header("Tether Components")]
    [SerializeField] private Transform anchoredPos;
    private float anchorMagnitude;
    private float anchorDistance;
    private Transform tetheredObj;
    private Rigidbody tetheredBody;

    [Header("Getters")]
    public Transform HoldPos => holdPos; 
    public Transform TetheredObj => tetheredObj;
    public Transform AnchoredPos => anchoredPos;
    public PlayerLassoState CurrentLassoState => currentLassoState;

    #region Unity Functions
    private void Awake()
    {
        playerActions = GetComponent<PlayerActions>();
        SwitchLassoState(PlayerLassoState.Empty);
        pullVelocityCurve = new AnimationCurve(
            new Keyframe(0f, 0f),          // start (no pull)
            new Keyframe(0.3f, 1.5f),      // overshoot above target
            new Keyframe(0.6f, 0.75f),     // dip below
            new Keyframe(0.8f, 1.25f),     // smaller overshoot
            new Keyframe(1f, 1f)           // settle exactly at target
        );
    }

    private void Update()
    {
        HandleLassoState();
    }
    #endregion

    #region State Handler

    private void HandleLassoState()
    {
        switch(currentLassoState)
        {
            case PlayerLassoState.Empty:
                HandleEmptyState();
                break;

            case PlayerLassoState.Firing:
                HandleFiringState();
                break;

            case PlayerLassoState.Anchored:
                HandleAnchoredState();
                break;

            case PlayerLassoState.Pulling:
                HandlePullState();
                break;

            case PlayerLassoState.Held:
                HandleHeldState();
                break;

        }
    }

    private void SwitchLassoState(PlayerLassoState newState)
    {
        currentLassoState = newState;
    }

    private void HandleEmptyState()
    {
        //if left click, start shooting projectile
        if (playerActions.MainDown)
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
            SwitchLassoState(PlayerLassoState.Empty);
        }
    }

    private void HandleAnchoredState()
    {
        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 maxDistancePos = ray.origin + ray.direction * anchorDistance;
        Vector3 minDistancePos = holdPos.position;
        anchoredPos.position = Vector3.Lerp(minDistancePos, maxDistancePos, anchorMagnitude);

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
        if(playerActions.MainDown)
        {
            ReleaseHeldObject();
            ResetAnchor();
            SwitchLassoState(PlayerLassoState.Empty);
        }

        //if right click, start pull
        if(playerActions.AltDown)
        {
            SwitchLassoState(PlayerLassoState.Pulling);
        }
    }

    private void HandlePullState()
    {
        //if you're holding right click, pull object towards you and snap when close enough
        if (playerActions.AltHeld)
        {
            tetheredBody.constraints = RigidbodyConstraints.None;
            float currentDistance = Vector3.Distance(anchoredPos.position, holdPos.position);
            anchorMagnitude = Mathf.Clamp01(currentDistance / anchorDistance);
            float normalizedProgress = 1f - anchorMagnitude;
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
                SwitchLassoState(PlayerLassoState.Held);
            }
        }

        //otherwise go back to anchor
        else if(playerActions.AltUp)
        {
            SwitchLassoState(PlayerLassoState.Anchored);
        }    
    }

    private void HandleHeldState()
    {
        //if you're holding the object, throw it away
        if (playerActions.AltDown)
        {
            ThrowHeldObject();
            ResetAnchor();
            SwitchLassoState(PlayerLassoState.Empty);
        }
        
        if(playerActions.MainDown)
        {
            ReleaseHeldObject();
            ResetAnchor();
            SwitchLassoState(PlayerLassoState.Empty);
        }
    }

    #endregion

    #region Projectile cast

    private void HandleProjectileStart()
    {
        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        projectilePosition = ray.origin;
        projectileVelocity = ray.direction * projectileSpeed;

        SwitchLassoState(PlayerLassoState.Firing);
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
            anchorDistance = Vector3.Distance(anchoredPos.position, holdPos.position);
            anchorMagnitude = 1f;
            SwitchLassoState(PlayerLassoState.Anchored);
        }
    }

    #endregion

    #region Pull

    private void PullObject(Transform target)
    {
        //Determines the location to send the object
        Vector3 objToHand = target.position - tetheredObj.position;
        Vector3 pullDir = objToHand.normalized;

        //Accelerates the objects velocity it's being pulled until the maxVelocity
        if (tetheredBody.linearVelocity.magnitude < maxVelocity)
            tetheredBody.AddForce(pullDir * tetherForceMultiplier, ForceMode.Force);
        else
            tetheredBody.linearVelocity = pullDir * maxVelocity;
    }
    #endregion

    #region Throwing / Holding
    private void ThrowHeldObject()
    {
        //Checks if there is a Tethered Object
        if (tetheredObj != null)
        {
            //Removes constraints on RigidBody
            tetheredBody.constraints = RigidbodyConstraints.None;

            //Adds Force to direction player is looking
            Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            tetheredBody.AddForce(ray.direction * throwVelocity, ForceMode.Impulse);

            //Reverts the object back to a regular object
            tetheredBody.useGravity = true;
            tetheredBody = null;
            tetheredObj.parent = null;
            tetheredObj = null;
        }
    }

    
    private void HoldObject(Transform targetPos)
    {
        //Places the object into a held position 
        tetheredObj.position = targetPos.position;
        tetheredObj.parent = targetPos;
        tetheredBody.MovePosition(targetPos.position);
        tetheredBody.constraints = RigidbodyConstraints.FreezePosition;
        tetheredBody.useGravity = false;
    }

    private void ReleaseHeldObject()
    {
        //Returns object to a neutral un-parented state where it's acting as a rigidBody again
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