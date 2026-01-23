using Unity.VisualScripting;
using UnityEngine;

public class ClawHead : EnvironmentalProp
{
    [Header("Claw Parameters")]
    [SerializeField] private float clawRetractSpeed = 10f;
    [SerializeField] private float maxClawLength = 20f;
    [SerializeField] private Transform clawBase;
    [SerializeField] private Transform machineBase;
    [SerializeField] private Transform clawModel;
    [SerializeField] private ConfigurableJoint clawBaseJoint;
    [SerializeField] private FixedJoint clawAttachmentJoint;
    [SerializeField] private Prop grabbedProp;
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Claw Joint")]
    [SerializeField] private float driveStrength = 100f;
    [SerializeField] private float driveDamper = 5f;
    [SerializeField] private float angularStrength = 100f;
    [SerializeField] private float angularDamper = 5f;

    private Rigidbody rb;

    public bool wasLetGo = false;

    private void Awake()
    {
        Init();
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        OnPropSnared += DeactivateJointForceOnGrab;
        OnPropReleased += ReactivateJointForceOnRelease;
    }

    protected override void Update()
    {
        base.Update();
        //clawModel.LookAt(machineBase, machineBase.up);

        lineRenderer.SetPosition(0, clawBase.position);
        lineRenderer.SetPosition(1, transform.position);

        if(grabbedProp != null)
        {
            if(IsSnared && wasLetGo)
            {
                DisconnectObjectWithClaw();
                wasLetGo = false;
            }
            if(IsSnared == false)
            {
                wasLetGo = true;
            }
        }
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
    }

    private void DeactivateJointForceOnGrab()
    {
        JointDrive xDrive = new JointDrive();
        JointDrive yDrive = new JointDrive();
        JointDrive zDrive = new JointDrive();
        JointDrive angularXDrive = new JointDrive();
        JointDrive angularYZDrive = new JointDrive();

        xDrive.positionSpring = 20f;
        xDrive.positionDamper = 0;
        xDrive.maximumForce = 50f;

        yDrive.positionSpring = 20f;
        yDrive.positionDamper = 0;
        yDrive.maximumForce = 50f;

        zDrive.positionSpring = 20f;
        zDrive.positionDamper = 0;
        zDrive.maximumForce = 50f;

        //angularXDrive.positionSpring = angularStrength;
        //angularXDrive.positionDamper = angularDamper;
        //angularXDrive.maximumForce = 100f;

        //angularYZDrive.positionSpring = angularStrength;
        //angularYZDrive.positionDamper = angularDamper;
        //angularYZDrive.maximumForce = 100f;

        clawBaseJoint.xDrive = xDrive;
        clawBaseJoint.yDrive = yDrive;
        clawBaseJoint.zDrive = zDrive;

        clawBaseJoint.angularXDrive = angularXDrive;
        clawBaseJoint.angularYZDrive = angularYZDrive;

        clawBaseJoint.autoConfigureConnectedAnchor = false;
        clawBaseJoint.enableCollision = true;
        clawBaseJoint.connectedAnchor = new Vector3(0, 0.3f, 0);

        clawBaseJoint.xMotion = ConfigurableJointMotion.Free;
        clawBaseJoint.yMotion = ConfigurableJointMotion.Free;
        clawBaseJoint.zMotion = ConfigurableJointMotion.Free;
    }

    private void ReactivateJointForceOnRelease()
    {
        JointDrive xDrive = new JointDrive();
        JointDrive yDrive = new JointDrive();
        JointDrive zDrive = new JointDrive();
        JointDrive angularXDrive = new JointDrive();
        JointDrive angularYZDrive = new JointDrive();

        xDrive.positionSpring = driveStrength;
        xDrive.positionDamper = driveDamper;
        xDrive.maximumForce = 200f;

        yDrive.positionSpring = driveStrength;
        yDrive.positionDamper = driveDamper;
        yDrive.maximumForce = 200f;

        zDrive.positionSpring = driveStrength;
        zDrive.positionDamper = driveDamper;
        zDrive.maximumForce = 200f;

        angularXDrive.positionSpring = angularStrength;
        angularXDrive.positionDamper = angularDamper;
        angularXDrive.maximumForce = 200f;

        angularYZDrive.positionSpring = angularStrength;
        angularYZDrive.positionDamper = angularDamper;
        angularYZDrive.maximumForce = 200f;

        clawBaseJoint.xDrive = xDrive;
        clawBaseJoint.yDrive = yDrive;
        clawBaseJoint.zDrive = zDrive;

        clawBaseJoint.angularXDrive = angularXDrive;
        clawBaseJoint.angularYZDrive = angularYZDrive;

        clawBaseJoint.autoConfigureConnectedAnchor = false;
        clawBaseJoint.enableCollision = true;
        clawBaseJoint.connectedAnchor = new Vector3(0, 0.3f, 0);

        clawBaseJoint.xMotion = ConfigurableJointMotion.Free;
        clawBaseJoint.yMotion = ConfigurableJointMotion.Free;
        clawBaseJoint.zMotion = ConfigurableJointMotion.Free;
    }

    private void OnGrabbedObjectSnared()
    {

    }

    private void ConnectObjectWithClaw(Transform connectedObject)
    {
        clawAttachmentJoint = connectedObject.AddComponent<FixedJoint>();
        clawAttachmentJoint.connectedBody = rb;
    }

    private void DisconnectObjectWithClaw()
    {
        Destroy(clawAttachmentJoint);
        clawAttachmentJoint = null;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision != null)
        {
            if(collision.gameObject.TryGetComponent<Prop>(out Prop prop))
            {
                ConnectObjectWithClaw(collision.transform);
                grabbedProp = prop;
            }
        }
    }
}
