using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;

public class ClawHead : EnvironmentalProp
{
    [Header("Claw Components")]
    [SerializeField] private Transform clawBase;
    [SerializeField] private Transform machineBase;
    [SerializeField] private Transform clawModel;
    [SerializeField] private Transform leftClaw;
    [SerializeField] private Transform rightClaw;
    [SerializeField] private ConfigurableJoint clawBaseJoint;
    [SerializeField] private FixedJoint clawAttachmentJoint;
    [SerializeField] private Prop grabbedProp;
    [SerializeField] public Prop currentSelectedProp;
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Editable Claw Params")]
    [SerializeField] private float clawRetractSpeed = 10f;
    [SerializeField] private float maxClawLength = 20f;
    [SerializeField] private bool isInteractable = true;
    [SerializeField] private bool attachOnStart = false;

    [Header("Editable Joint Params")]
    [SerializeField] private float driveStrength = 100f;
    [SerializeField] private float driveMax = 200f;
    [SerializeField] private float driveDamper = 5f;
    [SerializeField] private float angularStrength = 100f;
    [SerializeField] private float angularDamper = 5f;

    private Rigidbody rb;

    public bool isOpen = false;

    public event Action OnAttachToObject;
    public event Action OnDetachToObject;

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

        if (!isInteractable) return;

        if (IsSnared) isOpen = true;

        if(currentSelectedProp != null && clawAttachmentJoint == null)
        {
            if(IsSnared == false && isOpen)
            {
                ConnectObjectWithClaw(currentSelectedProp.transform);
                grabbedProp = currentSelectedProp;
            }

            if(isOpen) OpenClaw();
        }
        else CloseClaw();

        if (grabbedProp != null)
        {
            if (IsSnared)
            {
                DisconnectObjectWithClaw();
            }
        }

        if(!IsSnared) isOpen = false;
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
        xDrive.maximumForce = driveMax;

        yDrive.positionSpring = driveStrength;
        yDrive.positionDamper = driveDamper;
        yDrive.maximumForce = driveMax;

        zDrive.positionSpring = driveStrength;
        zDrive.positionDamper = driveDamper;
        zDrive.maximumForce = driveMax;

        angularXDrive.positionSpring = angularStrength;
        angularXDrive.positionDamper = angularDamper;
        angularXDrive.maximumForce = driveMax;

        angularYZDrive.positionSpring = angularStrength;
        angularYZDrive.positionDamper = angularDamper;
        angularYZDrive.maximumForce = driveMax;

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
        OnAttachToObject();
    }

    private void DisconnectObjectWithClaw()
    {
        Destroy(clawAttachmentJoint);
        clawAttachmentJoint = null;
        OnDetachToObject();
    }

    private void OpenClaw()
    {
        rightClaw.transform.localRotation = Quaternion.Euler(0, 0, 24f);
        leftClaw.transform.localRotation = Quaternion.Euler(0, 0, -24f);
        Debug.Log("open");
    }

    private void CloseClaw()
    {
        rightClaw.transform.localRotation = Quaternion.Euler(0, 0, 0f);
        leftClaw.transform.localRotation = Quaternion.Euler(0, 0, 0f);
        Debug.Log("close");
    }

    private void OnCollisionEnter(Collision collision)
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent<Prop>(out Prop prop))
        {
            if (prop.transform.GetComponent<ClawHead>() != null) return;

            currentSelectedProp = prop;

            if (attachOnStart) ConnectObjectWithClaw(prop.transform);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.TryGetComponent<Prop>(out Prop prop))
        {
            if (prop == currentSelectedProp)
            {
                currentSelectedProp = null;
            }
        }
    }

    public void DisableInteraction()
    {
        isInteractable = false;
    }
}
