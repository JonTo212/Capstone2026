using NodeCanvas.BehaviourTrees;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class JointTether : MonoBehaviour
{
    public delegate void TetherDestroyAction(JointTether jointTether);
    public event TetherDestroyAction OnTetherDestroy;

    private JointTetherVisuals tetherVisuals;
    private JointTetherCollider tetherCollider;
    [SerializeField] GameObject tetherRetrieveVisualsPrefab;

    public Transform playerTransform;

    [SerializeField] private GameObject trailRendererPrefab;
    private GameObject startTrailRenderer;
    private GameObject endTrailRenderer;

    [Header("Config Joint Parameters")]
    [SerializeField] private float driveStrength = 20f;
    [SerializeField] private float driveDamper = 5f;
    [SerializeField] private float maxForce = 250;
    //[SerializeField] private float breakForce = 750;
    [SerializeField] private float activationDelay = 0.4f;

    [Header("Properties")]
    [SerializeField] private bool isAutoActivate = false;
    public bool isActivated { get; private set; } = false;
    [SerializeField] private ConfigurableJoint joint;
    //[SerializeField] private ConfigurableJoint endJoint;
    private Rigidbody startRb;
    private Rigidbody endRb;
    private Transform startTransform;
    private Transform endTransform;
    private Transform startAnchor;
    private Transform endAnchor;
    //private GameObject temporaryStartRbObject;
    //private GameObject temporaryEndRbObject;
    private Vector3 startLocalPosition;
    private Vector3 endLocalPosition;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //StartCoroutine(DestroyTetherAfterTime());
    }
    public void Init(Transform startTransform, Vector3 startLocalPosition, Transform endTransform, Vector3 endLocalPosition, bool autoActivate)
    {
        this.startTransform = startTransform;
        this.startLocalPosition = startLocalPosition;
        this.endTransform = endTransform;
        this.endLocalPosition = endLocalPosition;

        //Try gets the rigitbody of the start and end transform. If there isn't a rigidbody, create a temporary rigidbody object as a target
        TryGetRigidbody(startTransform, startLocalPosition, out startRb, out startAnchor);
        TryGetRigidbody(endTransform, endLocalPosition, out endRb, out endAnchor);

        tetherVisuals = transform.GetComponent<JointTetherVisuals>();
        tetherVisuals.Init(startTransform, startLocalPosition, endTransform, endLocalPosition, isAutoActivate, activationDelay);

        tetherCollider = transform.GetComponent<JointTetherCollider>();
        tetherCollider.Init(startTransform, startLocalPosition, endTransform, endLocalPosition);

        AttachTether();

        if (autoActivate)
        {
            ActivateTether();
        }

        SoftJointLimit linearLimit = new SoftJointLimit();
        linearLimit.limit = (startTransform.TransformPoint(startLocalPosition) - endTransform.TransformPoint(endLocalPosition)).magnitude;

        joint.linearLimit = linearLimit;

        joint.xMotion = ConfigurableJointMotion.Limited;
        joint.yMotion = ConfigurableJointMotion.Limited;
        joint.zMotion = ConfigurableJointMotion.Limited;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void FixedUpdate()
    {
        MoveToTetherCenter();   
    }

    private void AttachTether()
    {
        tetherVisuals.SetLineColorActive();

        joint = CreateJoint(startRb, endRb);
        //endJoint = CreateJoint(endRb, startRb);

        CreateJointConnections(joint, startLocalPosition, endLocalPosition, startAnchor, endAnchor);
        //CreateJointConnections(endJoint, endLocalPosition, startLocalPosition, endAnchor, startAnchor);

        if (startTransform.GetComponent<Prop>() != null)
        {
            startTransform.GetComponent<Prop>().OnTetherPull(this, endAnchor, endTransform);
            startTransform.GetComponent<Prop>().OnPropDestroyed += DestroyTether;
        }
        if (endTransform.GetComponent<Prop>() != null)
        {
            endTransform.GetComponent<Prop>().OnTetherPull(this, startAnchor, startTransform);
            endTransform.GetComponent<Prop>().OnPropDestroyed += DestroyTether;
        }
    }

    public void ActivateTether()
    {
        if (joint == null /*|| endJoint == null*/) return;
        isActivated = true;
        StartCoroutine(ActivateTetherAfterDelay());
    }

    public void DeactivateTether()
    {
        Destroy(joint);
        //Destroy(endJoint);

        isActivated = false;

        tetherVisuals.SetLineColorInactive();
    }

    private void TryGetRigidbody(Transform fromTransform, Vector3 localHitPosition, out Rigidbody rb, out Transform anchorTransform)
    {
        if(fromTransform.gameObject.GetComponent<Rigidbody>() != null)
        {
            rb = fromTransform.gameObject.GetComponent<Rigidbody>();
            GameObject trail = Instantiate(trailRendererPrefab);
            trail.transform.parent = fromTransform;
            trail.transform.position= fromTransform.position;
            anchorTransform = fromTransform;
        }
        else
        {
            anchorTransform = new GameObject("TemporaryTetherRbObject").transform;
            anchorTransform.transform.position = fromTransform.TransformPoint(localHitPosition);
            anchorTransform.AddComponent<TemporaryJointAnchor>();
            rb = anchorTransform.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }
    }

    private ConfigurableJoint CreateJoint(Rigidbody sourceRb, Rigidbody targetRb)
    {
        ConfigurableJoint joint = sourceRb.gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = targetRb;
        joint.autoConfigureConnectedAnchor = false;
        joint.enableCollision = true;

        return joint;
    }

    private void ActivateJoint(ConfigurableJoint joint)
    {
        JointDrive xDrive = new JointDrive();
        JointDrive yDrive = new JointDrive();
        JointDrive zDrive = new JointDrive();

        xDrive.positionSpring = driveStrength;
        xDrive.positionDamper = driveDamper;
        xDrive.maximumForce = maxForce;

        yDrive.positionSpring = driveStrength;
        yDrive.positionDamper = driveDamper;
        yDrive.maximumForce = maxForce;

        zDrive.positionSpring = driveStrength;
        zDrive.positionDamper = driveDamper;
        zDrive.maximumForce = maxForce;

        joint.xDrive = xDrive;
        joint.yDrive = yDrive;
        joint.zDrive = zDrive;

        joint.autoConfigureConnectedAnchor = false;
        joint.enableCollision = true;
    }

    private void CreateJointConnections(ConfigurableJoint joint, Vector3 sourceLocalPosition, Vector3 targetLocalPosition,Transform sourceJointAnchor, Transform targetJointAnchor)
    {
        if (sourceJointAnchor.GetComponent<TemporaryJointAnchor>() == null) joint.anchor = sourceLocalPosition;
        else joint.anchor = Vector3.zero;

        if(targetJointAnchor.GetComponent<TemporaryJointAnchor>() == null) joint.connectedAnchor = targetLocalPosition;
        else joint.connectedAnchor = Vector3.zero;
    }

    private void MoveToTetherCenter()
    {
        if (startTransform == null || endTransform == null)
        {
            DestroyTether();
            return;
        }
        Vector3 worldStartPos = startTransform.TransformPoint(startLocalPosition);
        Vector3 worldEndPos = endTransform.TransformPoint(endLocalPosition);

        Vector3 startEndVector = worldEndPos - worldStartPos;
        
        transform.position = worldStartPos + startEndVector / 2;
    }

    public Vector3 GetCurrentForce(Rigidbody rb)
    {

        Vector3 startAnchorPos = startTransform.TransformPoint(startLocalPosition); 
        Vector3 endAnchorPos = endTransform.TransformPoint(endLocalPosition);

        Vector3 forceDirection;
        if (rb == startRb) forceDirection = (endAnchorPos - startAnchorPos).normalized;
        else forceDirection = (startAnchorPos - endAnchorPos).normalized;

        if(isActivated)
        {
            Vector3 jointDir = endAnchorPos - startAnchorPos;
            float dist = jointDir.magnitude;
            if (dist < 0.0001) return Vector3.zero;
            jointDir /= dist;

            float relativeVelocity = Vector3.Dot(endRb.linearVelocity - startRb.linearVelocity, jointDir);
            float scalarForce = Mathf.Clamp((driveStrength * dist) - (driveDamper * relativeVelocity), 0, maxForce);

            return forceDirection * scalarForce;
        }
        else
        {
            return joint.currentForce.magnitude * forceDirection;
        }
    }

    public void DestroyTether()
    {
        if (startTransform != null && startTransform.gameObject != null && startTransform.GetComponent<Prop>() != null)
        {
            startTransform.GetComponent<Prop>().OnDetachTether(this,endAnchor, endTransform);
        }
        if (endTransform != null && endTransform.gameObject != null && endTransform.GetComponent<Prop>() != null)
        {
            endTransform.GetComponent<Prop>().OnDetachTether(this, startAnchor, startTransform);
        }

        if(joint != null) Destroy(joint);
        //if(endJoint != null) Destroy(endJoint);

        if(startAnchor!= null && startAnchor.GetComponent<TemporaryJointAnchor>() != null) Destroy(startAnchor.gameObject);
        if (endAnchor != null && endAnchor.GetComponent<TemporaryJointAnchor>() != null) Destroy(endAnchor.gameObject);

        if(startTrailRenderer != null) Destroy(startTrailRenderer);
        if(endTrailRenderer != null) Destroy(endTrailRenderer);

        GameObject tetherRetrievalVisuals = Instantiate(tetherRetrieveVisualsPrefab, transform.position, Quaternion.Euler(Vector3.zero));
        tetherRetrievalVisuals.GetComponent<TetherRetrievalEffect>().Init(transform.position, playerTransform);

        OnTetherDestroy(this);
        Destroy(gameObject);
    }

    IEnumerator ActivateTetherAfterDelay()
    {
        tetherVisuals.SetLineColorActive();
        yield return new WaitForSeconds(activationDelay);

        ActivateJoint(joint);
        //ActivateJoint(endJoint);

        isActivated = true;
    }
}
