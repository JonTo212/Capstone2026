//using System.Collections;
//using System.Security.Cryptography;
//using Unity.VisualScripting;
//using UnityEngine;
//using UnityEngine.UIElements;

//public class JointLasso : MonoBehaviour
//{
//    [Header("External Components")]
//    [SerializeField] private Camera playerCam;
//    [SerializeField] private Transform holdPos;
//    [SerializeField] private AnimationCurve yankCurve;

//    [Header("Lasso Properties")]
//    [SerializeField] private float lassoRange;
//    [SerializeField] private float attachThreshold;
//    [SerializeField] private float pullStrength;
//    [SerializeField] private float throwStrength;

//    [Header("Configurable Joint Settings")]
//    [SerializeField] private float springForce = 600f;
//    private float damping;
//    private ConfigurableJoint holdJoint;
//    private Transform jointAnchor;

//    [Header("Spring Joint Settings")]
//    [SerializeField] private float springRate = 4.5f;
//    [SerializeField] private float massScale = 4.5f;
//    private SpringJoint grappleJoint;
//    private Transform grappleAnchor;

//    [Header("Internal Variables")]
//    private WeightComparison weightCompare;
//    private Prop snaredObj;
//    private Rigidbody playerRb;
//    private CapsuleCollider playerCol;
//    private Transform snaredObjTransform;
//    private Coroutine yankCoroutine;
//    private Coroutine grapplePullCoroutine;
//    private Vector3 anchorPoint;
//    private float anchorDistance;

//    [Header("Projectile Properties")]
//    [SerializeField] private float hitboxRadius;
//    [SerializeField] private float projectileSpeed;
//    private Vector3 projectilePosition;
//    private Vector3 projectileVelocity;
//    private Coroutine projectileCoroutine;

//    [Header("Getters")]
//    public bool HasSnaredObject => snaredObj != null;
//    public Transform SnaredObject => snaredObjTransform;
//    public Transform HoldPos => holdPos;
//    public Transform GrappleAnchor => grappleAnchor;
//    public Coroutine YankCoroutine => yankCoroutine;
//    public Coroutine ProjectileCoroutine => projectileCoroutine;
//    public Vector3 ProjectilePosition => projectilePosition;
//    public Vector3 AnchorPoint => anchorPoint;
//    public ConfigurableJoint HoldJoint => holdJoint;
//    public SpringJoint GrappleJoint => grappleJoint;

//    #region Unity Functions
//    private void Awake()
//    {
//        playerRb = GetComponent<Rigidbody>();
//        playerCol = GetComponent<CapsuleCollider>();
//        weightCompare = GetComponent<WeightComparison>();
//        yankCurve = new AnimationCurve(
//            new Keyframe(0f, 0f),
//            new Keyframe(0.1f, 2f),
//            new Keyframe(1f, 0f)
//            );
//    }

//    private void Update()
//    {
//        if(grappleAnchor != null)
//        {
//            grappleJoint.connectedAnchor = grappleAnchor.position;
//        }
//    }

//    #endregion

//    #region Helper Functions

//    public void MoveAnchorToCenter()
//    {
//        if (jointAnchor != null)
//        {
//            Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
//            Vector3 targetPos = ray.origin + ray.direction * anchorDistance;
//            jointAnchor.position = targetPos;
//        }
//    }

//    public void MoveAnchorToHand()
//    {
//        if (jointAnchor != null)
//        {
//            jointAnchor.position = holdPos.position;
//        }
//    }

//    #endregion

//    #region Start Lasso

//    public void TryLasso()
//    {
//        if (projectileCoroutine != null)
//            StopCoroutine(projectileCoroutine);

//        //projectileCoroutine = StartCoroutine(SimulateProjectile());

//        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
//        if (Physics.SphereCast(ray, hitboxRadius, out RaycastHit hit, lassoRange))
//        {
//            if (hit.transform.TryGetComponent(out Prop prop))
//            {
//                anchorDistance = Vector3.Distance(holdPos.position, hit.point);

//                var weightCheck = weightCompare.CompareObjectWeights(gameObject, hit.transform.gameObject);

//                if (hit.transform.TryGetComponent(out IActivatable activatable))
//                {
//                    activatable.Activate();
//                }

//                switch (weightCheck)
//                {
//                    case WeightComparisonResult.Object1:
//                        SetUpConfigurableJoint(prop.Rb, hit.point, holdPos.parent); //player is heavier, so make the object move to center of screen
//                        snaredObj = prop;
//                        snaredObjTransform = hit.transform;
//                        snaredObj.OnSnare();
//                        break;

//                    case WeightComparisonResult.Object2:
//                        SetUpSpringJoint(gameObject, transform.position, hit.point); //object is heavier, make a spring joint for grapple-like effect
//                        grappleAnchor = hit.transform;
//                        snaredObjTransform = hit.transform;
//                        break;

//                    case WeightComparisonResult.Equal:
//                        SetUpConfigurableJoint(prop.Rb, hit.transform.position, hit.transform); //temporary
//                        snaredObj = prop;
//                        snaredObjTransform = hit.transform;
//                        snaredObj.OnSnare();
//                        break;
//                }
//            }
//        }
//    }

//    private IEnumerator SimulateProjectile()
//    {
//        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
//        projectilePosition = ray.origin;
//        projectileVelocity = ray.direction * projectileSpeed;

//        while (Vector3.Distance(holdPos.position, projectilePosition) < lassoRange)
//        {
//            projectilePosition += projectileVelocity * Time.deltaTime;

//            if (Physics.SphereCast(projectilePosition, hitboxRadius, projectileVelocity.normalized, out RaycastHit hit, projectileVelocity.magnitude))
//            {
//                if (hit.transform.TryGetComponent(out ISnareable snareable))
//                {
//                    anchorDistance = Vector3.Distance(holdPos.position, hit.point);

//                    var weightCheck = weightCompare.CompareObjectWeights(gameObject, hit.transform.gameObject);

//                    switch (weightCheck)
//                    {
//                        case WeightComparisonResult.Object1:
//                            SetUpConfigurableJoint(snareable.Rb, hit.transform.position, hit.transform); //player is heavier, so make the object move to center of screen
//                            snaredObjTransform = hit.transform;
//                            break;

//                        case WeightComparisonResult.Object2:
//                            SetUpSpringJoint(gameObject, holdPos.position, hit.point); //object is heavier, make a spring joint for grapple-like effect
//                            grappleAnchor = hit.transform;
//                            break;

//                        case WeightComparisonResult.Equal:
//                            SetUpSpringJoint(gameObject, holdPos.position, hit.point); //change this to pull objects together
//                            break;
//                    }
//                }
//                projectileCoroutine = null;
//                yield break; //stop coroutine on hit
//            }
//            yield return null;
//        }
//        projectileCoroutine = null;
//    }
//    #endregion

//    #region Joint Setup
//    private void SetUpSpringJoint(GameObject jointObj, Vector3 startPos, Vector3 anchorPos)
//    {
//        anchorPoint = anchorPos;

//        SpringJoint joint = jointObj.AddComponent<SpringJoint>();
//        joint.autoConfigureConnectedAnchor = false;
//        joint.connectedAnchor = anchorPoint;

//        float currentDist = Vector3.Distance(startPos, anchorPos);
//        joint.maxDistance = currentDist * 0.8f;
//        joint.minDistance = currentDist * 0.25f;

//        joint.spring = springRate;
//        float damping = 2f * Mathf.Sqrt(joint.spring * playerRb.mass);
//        joint.damper = damping;
//        joint.massScale = massScale;

//        grappleJoint = joint;
//    }

//    private void SetUpConfigurableJoint(Rigidbody rb, Vector3 attachmentPosition, Transform parent)
//    {
//        GameObject go = new GameObject("JointAnchor");
//        go.transform.position = attachmentPosition;
//        go.transform.parent = parent;

//        //this rb acts as anchor
//        Rigidbody newRb = go.AddComponent<Rigidbody>();
//        newRb.isKinematic = true;

//        //make a new joint that uses jointdrive (like addForce in a direction, with damping)
//        damping = 2f * Mathf.Sqrt(springForce * playerRb.mass);
//        ConfigurableJoint joint = go.AddComponent<ConfigurableJoint>();
//        joint.connectedBody = rb;
//        joint.configuredInWorldSpace = true;
//        joint.xDrive = NewJointDrive(springForce, damping);
//        joint.yDrive = NewJointDrive(springForce, damping);
//        joint.zDrive = NewJointDrive(springForce, damping);
//        joint.slerpDrive = NewJointDrive(springForce, damping);
//        joint.rotationDriveMode = RotationDriveMode.Slerp;

//        jointAnchor = go.transform;
//        holdJoint = joint;
//    }

//    private JointDrive NewJointDrive(float force, float damping)
//    {
//        JointDrive drive = new JointDrive();
//        drive.positionSpring = force;
//        drive.positionDamper = damping;
//        drive.maximumForce = Mathf.Infinity;
//        return drive;
//    }

//    #endregion

//    #region Release and Yank

//    public void YankObject()
//    {
//        if (snaredObj != null)
//        {
//            if (yankCoroutine != null)
//                StopCoroutine(yankCoroutine);

//            yankCoroutine = StartCoroutine(Yank());
//        }
//    }

//    private Vector3 GetAnimCurvePoint(Vector3 start, Vector3 end, float height, float t)
//    {
//        Vector3 mid = Vector3.Lerp(start, end, t);
//        float curveFactor = yankCurve.Evaluate(t);
//        mid.y += curveFactor * height;

//        return mid;
//    }

//    private Vector3 CalculateYankVelocityExact(Vector3 start, Vector3 end, float flightTime) //no control over Y
//    {
//        Vector3 displacement = end - start;
//        float gravity = Physics.gravity.y;

//        Vector3 velocityXZ = new Vector3(displacement.x / flightTime, 0f, displacement.z / flightTime);
//        float velocityY = (displacement.y - 0.5f * gravity * flightTime * flightTime) / flightTime;

//        return velocityXZ + Vector3.up * velocityY;
//    }

//    private IEnumerator Yank()
//    {
//        float totalYankDuration = 0.5f;
//        float startTime = Time.time;

//        while (Vector3.Distance(snaredObjTransform.position, holdPos.position) > attachThreshold)
//        {
//            float elapsedTime = Time.time - startTime;
//            float remainingTime = totalYankDuration - elapsedTime;
//            float clampedRemainingTime = Mathf.Max(remainingTime, 0.05f);

//            Vector3 idealVelocity = CalculateYankVelocityExact(jointAnchor.position, holdPos.position, clampedRemainingTime);
//            jointAnchor.position += idealVelocity * Time.deltaTime;

//            yield return null;
//        }

//        snaredObj.OnHold(holdPos);
//        snaredObj.AttachedTransform = transform;
//        yankCoroutine = null;
//    }

//    public void PullTowardsAnchor()
//    {
//        if(grappleJoint != null)
//        {
//            if(grapplePullCoroutine != null)
//                StopCoroutine(grapplePullCoroutine);

//            grapplePullCoroutine = StartCoroutine(ShortenGrapple());
//        }
//    }

//    private Vector3 CalculateYankVelocity(Vector3 start, Vector3 end, float peakHeight) //original grapple function, slow
//    {
//        float gravity = -GetComponent<PlayerController>().Gravity;
//        float displacementY = end.y - start.y;

//        float timeUp = Mathf.Sqrt(-2f * peakHeight / gravity);
//        float timeDown = Mathf.Sqrt(2f * (displacementY - peakHeight) / gravity);
//        float totalTime = timeUp + timeDown;

//        Vector3 displacementXZ = new Vector3(end.x - start.x, 0f, end.z - start.z);

//        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2f * gravity * peakHeight);
//        Vector3 velocityXZ = displacementXZ / totalTime;

//        return velocityXZ + velocityY;
//    }

//    private Vector3 GetBallisticVelocityAtTime(Vector3 start, Vector3 end, float peakHeight, float t)
//    {
//        float gravity = -GetComponent<PlayerController>().Gravity;
//        Vector3 launchVel = CalculateYankVelocity(start, end, peakHeight);
//        Vector3 velAtTime = launchVel + Vector3.up * gravity * t;

//        return velAtTime;
//    }

//    private IEnumerator ShortenGrapple()
//    {
//        Vector3 lowestPoint = new Vector3(transform.position.x, transform.position.y - (playerCol.height / 2), transform.position.z);
//        Vector3 aboveObj = snaredObjTransform.position + Vector3.up * (snaredObjTransform.GetComponent<Collider>().bounds.extents.y + playerCol.height / 2f);

//        float grapplePointRelativeYPos = snaredObjTransform.position.y - lowestPoint.y;
//        float highestPointOnArc = grapplePointRelativeYPos + 2f;
//        if (grapplePointRelativeYPos < 0) highestPointOnArc = 2f;

//        Vector3 launchVel = CalculateYankVelocity(transform.position, aboveObj, highestPointOnArc);
//        Destroy(grappleJoint);
//        grappleAnchor = null;
//        playerRb.linearVelocity = launchVel;

//        while (Vector3.Distance(snaredObjTransform.position, transform.position) > attachThreshold)
//        {
//            yield return null;
//        }

//        grapplePullCoroutine = null;
//    }

//    public void ThrowObject()
//    {
//        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
//        ReleaseObject(true);
//        snaredObj.OnThrow(ray.direction, throwStrength);
//        snaredObj = null;
//    }

//    public void ReleaseObject(bool throwObj)
//    {
//        if (holdJoint != null)
//        {
//            if (yankCoroutine != null)
//                StopCoroutine(yankCoroutine);

//            snaredObj.OnRelease();
//            Destroy(holdJoint);
//            Destroy(jointAnchor.gameObject);
//            holdJoint = null;
//            jointAnchor = null;
//            if (!throwObj) 
//                snaredObj = null;
//        }
//    }

//    public void ReleaseGrapple()
//    {
//        if (grapplePullCoroutine != null)
//            StopCoroutine(grapplePullCoroutine);

//        Destroy(grappleJoint);
//        grappleJoint = null;
//        grappleAnchor = null;
//    }

//    #endregion
//}
