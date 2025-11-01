using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class JointTether : MonoBehaviour
{
    public delegate void TetherDestroyAction(JointTether jointTether);
    public event TetherDestroyAction OnTetherDestroy;

    private JointTetherVisuals tetherVisuals;
    private JointTetherCollider tetherCollider;

    [SerializeField] private GameObject trailRendererPrefab;
    private GameObject startTrailRenderer;
    private GameObject endTrailRenderer;

    [Header("Config Joint Parameters")]
    [SerializeField] private float driveStrength = 20f;
    [SerializeField] private float driveDamper = 5f;
    [SerializeField] private float activationDelay = 0.4f;

    [Header("Properties")]
    [SerializeField] private bool isAutoActivate = false;
    public bool isActivated { get; private set; } = false;
    [SerializeField] private ConfigurableJoint startJoint;
    [SerializeField] private ConfigurableJoint endJoint;
    private Rigidbody startRb;
    private Rigidbody endRb;
    public Transform startTransform { get; private set; }
    public Transform endTransform { get; private set; }
    private GameObject temporaryStartRbObject;
    private GameObject temporaryEndRbObject;
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
        TryGetRigidbody(startTransform, startLocalPosition, out startRb, out temporaryStartRbObject);
        TryGetRigidbody(endTransform, endLocalPosition, out endRb, out temporaryEndRbObject);

        tetherVisuals = transform.GetComponent<JointTetherVisuals>();
        tetherVisuals.Init(startTransform, startLocalPosition, endTransform, endLocalPosition, isAutoActivate, activationDelay);

        tetherCollider = transform.GetComponent<JointTetherCollider>();
        tetherCollider.Init(startTransform, startLocalPosition, endTransform, endLocalPosition);
        
        if(autoActivate)
        {
            ActivateTether();
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void FixedUpdate()
    {
        MoveToTetherCenter();   
    }

    public void ActivateTether()
    {
        if (startJoint != null && endJoint != null) return;

        StartCoroutine(ActivateTetherAfterDelay());
    }

    public void DeactivateTether()
    {
        Destroy(startJoint);
        Destroy(endJoint);

        isActivated = false;

        tetherVisuals.SetLineColorInactive();
    }

    private void TryGetRigidbody(Transform fromTransform, Vector3 localHitPosition, out Rigidbody rb, out GameObject temporaryRbObject)
    {
        if(fromTransform.gameObject.GetComponent<Rigidbody>() != null)
        {
            rb = fromTransform.gameObject.GetComponent<Rigidbody>();
            GameObject trail = Instantiate(trailRendererPrefab);
            trail.transform.parent = fromTransform;
            trail.transform.position= fromTransform.position;
            temporaryRbObject = null;
        }
        else
        {
            temporaryRbObject = new GameObject("TemporaryTetherRbObject");
            temporaryRbObject.transform.position = fromTransform.TransformPoint(localHitPosition);
            rb = temporaryRbObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }
    }

    private ConfigurableJoint CreateJoint(Rigidbody sourceRb, Rigidbody targetRb)
    {
        ConfigurableJoint joint = sourceRb.gameObject.AddComponent<ConfigurableJoint>();

        joint.connectedBody = targetRb;

        JointDrive xDrive = new JointDrive();
        JointDrive yDrive = new JointDrive();
        JointDrive zDrive = new JointDrive();   

        xDrive.positionSpring = driveStrength;
        xDrive.positionDamper = driveDamper;
        xDrive.maximumForce = 1000000f;

        yDrive.positionSpring = driveStrength;
        yDrive.positionDamper = driveDamper;
        yDrive.maximumForce = 1000000f;

        zDrive.positionSpring = driveStrength;
        zDrive.positionDamper = driveDamper; 
        zDrive.maximumForce = 1000000f;

        joint.xDrive = xDrive;
        joint.yDrive = yDrive;
        joint.zDrive = zDrive;

        joint.autoConfigureConnectedAnchor = false;
        joint.enableCollision = true;

        return joint;
    }

    private void CreateJointConnections(ConfigurableJoint joint, Vector3 sourceLocalPosition, Vector3 targetLocalPosition,GameObject temporaryRbSource, GameObject temporaryRbTarget)
    {
        if (temporaryRbSource == null) joint.anchor = sourceLocalPosition;
        else joint.anchor = Vector3.zero;

        if(temporaryRbTarget == null) joint.connectedAnchor = targetLocalPosition;
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

    public void DestroyTether()
    {
        if (startTransform != null && startTransform.gameObject != null && startTransform.GetComponent<Prop>() != null)
        {
            startTransform.GetComponent<Prop>().OnDetachTether(gameObject, endTransform, startJoint);
        }
        if (endTransform != null && endTransform.gameObject != null && endTransform.GetComponent<Prop>() != null)
        {
            endTransform.GetComponent<Prop>().OnDetachTether(gameObject, startTransform, endJoint);
        }

        if(startJoint != null) Destroy(startJoint);
        if(endJoint != null) Destroy(endJoint);

        if(temporaryStartRbObject != null) Destroy(temporaryStartRbObject);
        if(temporaryEndRbObject != null) Destroy(temporaryEndRbObject);

        if(startTrailRenderer != null) Destroy(startTrailRenderer);
        if(endTrailRenderer != null) Destroy(endTrailRenderer); 

        OnTetherDestroy(this);
        Destroy(gameObject);
    }

    IEnumerator ActivateTetherAfterDelay()
    {
        tetherVisuals.SetLineColorActive();
        yield return new WaitForSeconds(activationDelay);

        startJoint = CreateJoint(startRb, endRb);
        endJoint = CreateJoint(endRb, startRb);

        CreateJointConnections(startJoint, startLocalPosition, endLocalPosition, temporaryStartRbObject, temporaryEndRbObject);
        CreateJointConnections(endJoint, endLocalPosition, startLocalPosition, temporaryEndRbObject, temporaryStartRbObject);

        if (startTransform.GetComponent<Prop>() != null)
        {
            startTransform.GetComponent<Prop>().OnTetherPull(gameObject,endTransform, startJoint);
            startTransform.GetComponent<Prop>().OnPropDestroyed += DestroyTether;
        }
        if (endTransform.GetComponent<Prop>() != null)
        {
            endTransform.GetComponent<Prop>().OnTetherPull(gameObject,endTransform, endJoint);
            startTransform.GetComponent<Prop>().OnPropDestroyed += DestroyTether;
        }

        isActivated = true;
    }
}
