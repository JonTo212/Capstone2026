using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(HingeJoint))]
public class DoorHingeLock : MonoBehaviour
{
    private enum DoorOpenState
    {
        Min,
        Neutral,
        Max
    }

    private HingeJoint _hingeJoint;

    [Header("Spring Settings")]
    [SerializeField] private bool useSpring = true;
    [SerializeField] private float springForce = 10f;
    [SerializeField] private float dampeningAmount = 5f;

    [Header("Rotation Lock Settings")]
    [SerializeField] private float minRotation = -120f;
    [SerializeField] private float maxRotation = 120f;
    [SerializeField] private float lockThreshold = 20f;
    private DoorOpenState currentDoorOpenState = DoorOpenState.Neutral;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _hingeJoint = GetComponent<HingeJoint>();
        _hingeJoint.useSpring = useSpring;
        _hingeJoint.useLimits = true;

        JointLimits newLimits = new JointLimits();
        newLimits.min = minRotation;
        newLimits.max = maxRotation;

        _hingeJoint.limits = newLimits;
        _hingeJoint.spring = NewJointSpring(0);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float localYRotAngle = transform.localRotation.y * 360;
        float minThreshold = minRotation + lockThreshold;
        float maxThreshold = maxRotation - lockThreshold;

        if(localYRotAngle < minThreshold && currentDoorOpenState != DoorOpenState.Min)
        {
            _hingeJoint.spring = NewJointSpring(minRotation);
            currentDoorOpenState = DoorOpenState.Min;
            return;
        }
        else if(localYRotAngle > maxThreshold && currentDoorOpenState != DoorOpenState.Max) 
        {
            _hingeJoint.spring = NewJointSpring(maxRotation);
            currentDoorOpenState = DoorOpenState.Max;
            return;
        }
        else if(currentDoorOpenState != DoorOpenState.Neutral && localYRotAngle < maxThreshold && localYRotAngle > minThreshold) 
        {
            _hingeJoint.spring = NewJointSpring(0);
            currentDoorOpenState = DoorOpenState.Neutral;
            return;
        }
    }

    private JointSpring NewJointSpring(float targetPositon)
    {
        JointSpring jointSpring = new JointSpring();
        jointSpring.targetPosition = targetPositon;
        jointSpring.spring = springForce;
        jointSpring.damper = dampeningAmount;

        return jointSpring;
    }
}
