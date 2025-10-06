using NodeCanvas.Tasks.Actions;
using System.Collections;
using UnityEditor.Build;
using UnityEngine;

public class JointTether : MonoBehaviour
{
    public delegate void TetherDestroyAction();
    public event TetherDestroyAction OnTetherDestroy;
   
    private JointTetherVisuals tetherVisuals;

    [Header("Config Joint Parameters")]
    [SerializeField] private float driveStrength = 20f;
    [SerializeField] private float driveDamper = 5f;

    [Header("Properties")]
    private ConfigurableJoint startJoint;
    private ConfigurableJoint endJoint;
    private Rigidbody startRb;
    private Rigidbody endRb;
    private Transform startTransform;
    private Transform endTransform;
    private GameObject temporaryStartRbObject;
    private GameObject temporaryEndRbObject;
    private Vector3 startLocalPosition;
    private Vector3 endLocalPosition;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(DestroyTetherAfterTime());
    }
    public void Init(Transform startTransform, Vector3 startLocalPosition, Transform endTransform, Vector3 endLocalPosition)
    {
        this.startTransform = startTransform;
        this.startLocalPosition = startLocalPosition;
        this.endTransform = endTransform;
        this.endLocalPosition = endLocalPosition;

        //Try gets the rigitbody of the start and end transform. If there isn't a rigidbody, create a temporary rigidbody object as a target
        TryGetRigidbody(startTransform, startLocalPosition, out startRb, out temporaryStartRbObject);
        TryGetRigidbody(endTransform, endLocalPosition, out endRb, out temporaryEndRbObject);

        startJoint = CreateJoint(startRb, endRb);
        endJoint = CreateJoint(endRb, startRb);

        startJoint.anchor = startLocalPosition;

        if (temporaryStartRbObject == null) startJoint.connectedAnchor = endLocalPosition;
        else startJoint.connectedAnchor = Vector3.zero;

        endJoint.anchor = endLocalPosition;

        if (temporaryStartRbObject == null) endJoint.connectedAnchor = endLocalPosition;
        else endJoint.connectedAnchor = Vector3.zero;

        //CreateJointConnections(startJoint, startLocalPosition, endLocalPosition, temporaryStartRbObject);
        //CreateJointConnections(endJoint, endLocalPosition, startLocalPosition, temporaryEndRbObject);

        tetherVisuals = transform.GetComponent<JointTetherVisuals>();
        tetherVisuals.Init(startTransform, startLocalPosition, endTransform, endLocalPosition, true); 
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        
    }

    private void TryGetRigidbody(Transform fromTransform, Vector3 localHitPosition, out Rigidbody rb, out GameObject temporaryRbObject)
    {
        if(fromTransform.gameObject.GetComponent<Rigidbody>() != null)
        {
            rb = fromTransform.gameObject.GetComponent<Rigidbody>();
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

    private void CreateJointConnections(ConfigurableJoint joint, Vector3 sourceLocalPosition, Vector3 targetLocalPosition, GameObject temporaryRbObject)
    {
        joint.anchor = sourceLocalPosition;

        if(temporaryRbObject == null) joint.connectedAnchor = targetLocalPosition;
        else joint.connectedAnchor = Vector3.zero;
    }

    public void DestroyTether()
    {
        OnTetherDestroy();

        if(temporaryStartRbObject != null) Destroy(temporaryStartRbObject);
        if(temporaryEndRbObject != null) Destroy(temporaryEndRbObject);

        Destroy(startJoint);
        Destroy(endJoint);

        Destroy(gameObject);
    }

    IEnumerator DestroyTetherAfterTime()
    {
        yield return new WaitForSeconds(5.0f);

        DestroyTether();
    }
}
