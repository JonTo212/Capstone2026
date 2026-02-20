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
    [SerializeField] private float angularDriveStrength = 5f;
    [SerializeField] private float angularDamper = 2f;

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
    private Vector3 startLocalPosition;
    private Vector3 endLocalPosition;

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

        joint.angularXMotion = ConfigurableJointMotion.Free;
        joint.angularYMotion = ConfigurableJointMotion.Free;
        joint.angularZMotion = ConfigurableJointMotion.Free;
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

        Quaternion anchorRot = (endRb != null) ? endRb.transform.rotation : Quaternion.identity;
        Quaternion currentRelativeRot = Quaternion.Inverse(anchorRot) * startRb.transform.rotation;
        joint.targetRotation = Quaternion.Inverse(currentRelativeRot);

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
            //GameObject trail = Instantiate(trailRendererPrefab);
            //trail.transform.parent = fromTransform;
            //trail.transform.position= fromTransform.position;
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
        if(targetRb.transform.GetComponent<NPC_Pufferfish>() != null)
        {
            FlipStartEndVariables();

            Rigidbody tempSource = sourceRb;
            sourceRb = targetRb;
            targetRb = tempSource;
        }
        ConfigurableJoint joint = sourceRb.gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = targetRb;
        joint.autoConfigureConnectedAnchor = false;
        joint.enableCollision = true;

        if (sourceRb.transform.GetComponent<NPC_Pufferfish>() != null)
        {
            BigMamaJoint (joint);
        }

        return joint;
    }

    private void FlipStartEndVariables()
    {
        Rigidbody storedStartRb = startRb;
        Transform storedStartTransform = startTransform;
        Transform storedStartAnchor = startAnchor;
        Vector3 storedStartLocalPosition = startLocalPosition;

        startRb = endRb;
        startTransform = endTransform;
        startAnchor = endAnchor;
        startLocalPosition = endLocalPosition;

        endRb = storedStartRb;
        endTransform = storedStartTransform;
        endAnchor = storedStartAnchor;
        endLocalPosition = storedStartLocalPosition;
    }

    public void ActivateJoint()
    {
        JointDrive xDrive = new JointDrive();
        JointDrive yDrive = new JointDrive();
        JointDrive zDrive = new JointDrive();
        JointDrive angularXDrive = new JointDrive();
        JointDrive angularYZDrive = new JointDrive();

        xDrive.positionSpring = driveStrength;
        xDrive.positionDamper = driveDamper;
        xDrive.maximumForce = maxForce;

        yDrive.positionSpring = driveStrength;
        yDrive.positionDamper = driveDamper;
        yDrive.maximumForce = maxForce;

        zDrive.positionSpring = driveStrength;
        zDrive.positionDamper = driveDamper;
        zDrive.maximumForce = maxForce;

        angularXDrive.positionSpring = angularDriveStrength;
        angularXDrive.positionDamper = angularDamper;
        angularXDrive.maximumForce = 100f;

        angularYZDrive.positionSpring = angularDriveStrength;
        angularYZDrive.positionDamper = angularDamper;
        angularYZDrive.maximumForce = 100f;

        joint.xDrive = xDrive;
        joint.yDrive = yDrive;
        joint.zDrive = zDrive;

        joint.angularXDrive = angularXDrive;
        joint.angularYZDrive = angularYZDrive;

        joint.autoConfigureConnectedAnchor = false;
        joint.enableCollision = true;

        joint.xMotion = ConfigurableJointMotion.Free;
        joint.yMotion = ConfigurableJointMotion.Free;
        joint.zMotion = ConfigurableJointMotion.Free;
    }

    public void DisableJoint()
    {
        if (joint == null) return;

        JointDrive xDrive = new JointDrive();
        JointDrive yDrive = new JointDrive();
        JointDrive zDrive = new JointDrive();
        JointDrive angularXDrive = new JointDrive();
        JointDrive angularYZDrive = new JointDrive();

        xDrive.positionSpring = 0f;
        xDrive.positionDamper = 0f;
        xDrive.maximumForce = 0f;

        yDrive.positionSpring = 0f;
        yDrive.positionDamper = 0f;
        yDrive.maximumForce = 0f;

        zDrive.positionSpring = 0f;
        zDrive.positionDamper = 0f;
        zDrive.maximumForce = 0f;

        angularXDrive.positionSpring = 0f;
        angularXDrive.positionDamper = 0f;
        angularXDrive.maximumForce = 0f;

        angularYZDrive.positionSpring = 0f;
        angularYZDrive.positionDamper = 0f;
        angularYZDrive.maximumForce = 0f;

        joint.xDrive = xDrive;
        joint.yDrive = yDrive;
        joint.zDrive = zDrive;

        joint.angularXDrive = angularXDrive;
        joint.angularYZDrive = angularYZDrive;

        joint.autoConfigureConnectedAnchor = false;
        joint.enableCollision = true;
    }

    private void BigMamaJoint(ConfigurableJoint joint)
    {
        JointDrive xDrive = new JointDrive();
        JointDrive yDrive = new JointDrive();
        JointDrive zDrive = new JointDrive();
        JointDrive angularXDrive = new JointDrive();
        JointDrive angularYZDrive = new JointDrive();

        xDrive.positionSpring = 50f;
        xDrive.positionDamper = 2;
        xDrive.useAcceleration = true;
        xDrive.maximumForce = 1000f;

        zDrive.positionSpring = 50f;
        zDrive.positionDamper = 2f;
        zDrive.useAcceleration = true;
        zDrive.maximumForce = 1000f;

        angularXDrive.positionSpring = 50f;
        angularXDrive.positionDamper = 2f;
        angularXDrive.useAcceleration = true;
        angularXDrive.maximumForce = 1000f;

        angularYZDrive.positionSpring = 50f;
        angularYZDrive.positionDamper = 2f;
        angularYZDrive.useAcceleration = true;
        angularYZDrive.maximumForce = 1000f;

        joint.xDrive = xDrive;
        joint.yDrive = yDrive;
        joint.zDrive = zDrive;

        joint.angularXDrive = angularXDrive;
        joint.angularYZDrive = angularYZDrive;

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

        float mass = rb.mass;

        if(isActivated)
        {
            Vector3 jointDir = endAnchorPos - startAnchorPos;
            float dist = jointDir.magnitude;
            if (dist < 0.0001) return Vector3.zero;
            jointDir /= dist;

            float relativeVelocity = Vector3.Dot(endRb.linearVelocity - startRb.linearVelocity, jointDir);
            float scalarForce = Mathf.Clamp((driveStrength * dist) - (driveDamper * relativeVelocity), 0, maxForce);

            return forceDirection * scalarForce * mass;
        }
        else
        {
            return joint.currentForce.magnitude * forceDirection * mass;
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

       // if(startTrailRenderer != null) Destroy(startTrailRenderer);
        //if(endTrailRenderer != null) Destroy(endTrailRenderer);

        GameObject tetherRetrievalVisuals = Instantiate(tetherRetrieveVisualsPrefab, transform.position, Quaternion.Euler(Vector3.zero));
        tetherRetrievalVisuals.GetComponent<TetherRetrievalEffect>().Init(transform.position, playerTransform);

        OnTetherDestroy(this);
        Destroy(gameObject);
    }

    public void OnWeightIncrease(Rigidbody rbTarget)
    {
        if(rbTarget == startRb)
        {
            joint.massScale = 10f;
        }
        if(rbTarget == endRb)
        {
            joint.connectedMassScale = 10f;
        }
    }

    public void OnWeightReset(Rigidbody rbTarget)
    {
        if (rbTarget == startRb)
        {
            joint.massScale = 1f;
        }
        if (rbTarget == endRb)
        {
            joint.connectedMassScale = 1f;
        }
    }
    public void UpdateTetherRotation(Quaternion desiredWorldRot)
    {
        if (joint == null || startRb == null) return;

        joint.rotationDriveMode = RotationDriveMode.XYAndZ;

        //we're passing in the rotation at the time of release
        //find the anchor's rotation
        Quaternion anchorRot = Quaternion.identity;
        if (endRb != null)
        {
            anchorRot = endRb.transform.rotation;
        }

        //convert it to local space of the anchor
        Quaternion relativeRot = Quaternion.Inverse(anchorRot) * desiredWorldRot;

        //targetRotation is the inverse of desired local rotation
        joint.targetRotation = Quaternion.Inverse(relativeRot);
    }


    public void UpdateGrabPointToNearest()
    {
        if (joint == null || startRb == null) return;

        Prop startProp = startTransform.GetComponent<Prop>();

        //get tether direction
        Vector3 endWorldPos = endTransform.TransformPoint(endLocalPosition);
        Vector3 objectCenter = startRb.worldCenterOfMass;
        Vector3 tetherDirection = (endWorldPos - objectCenter).normalized;

        Vector3 newWorldPosition = Vector3.zero;
        bool foundPosition = false;

        // First try to use grab points if they exist
        if (startProp != null && startProp.GrabPoints != null && startProp.GrabPoints.Count > 0)
        {
            //find nearest grab point to the alignment of the tether
            Transform bestGrabPoint = null;
            float bestAlignment = float.MinValue;

            foreach (Transform grabPoint in startProp.GrabPoints)
            {
                Vector3 grabPointDir = (grabPoint.position - objectCenter).normalized;
                float alignment = Vector3.Dot(grabPointDir, tetherDirection);

                if (alignment > bestAlignment)
                {
                    bestAlignment = alignment;
                    bestGrabPoint = grabPoint;
                }
            }

            if (bestGrabPoint != null)
            {
                newWorldPosition = bestGrabPoint.position;
                foundPosition = true;
            }
        }

        // Fallback: if no grab points, find closest point on surface
        if (!foundPosition)
        {
            newWorldPosition = FindClosestPointOnSurface(startTransform, objectCenter, tetherDirection);
            foundPosition = true;
        }

        if (foundPosition)
        {
            //update start pos, anchor and visuals
            startLocalPosition = startTransform.InverseTransformPoint(newWorldPosition);
            joint.anchor = startLocalPosition;
            tetherVisuals.Init(startTransform, startLocalPosition, endTransform, endLocalPosition, isActivated, activationDelay);
        }
    }

    private Vector3 FindClosestPointOnSurface(Transform objTransform, Vector3 objectCenter, Vector3 direction)
    {
        // We want to find the face most aligned with the tether direction, then use its center

        Collider[] colliders = objTransform.GetComponentsInChildren<Collider>();

        if (colliders.Length == 0)
        {
            // No colliders found, return a point on a sphere approximation
            return objectCenter + direction * 0.5f;
        }

        // Cast multiple rays from outside the object to sample face normals and positions
        Vector3 bestFaceCenter = objectCenter;
        float bestAlignment = float.MinValue;
        int hitCount = 0;

        // Sample rays in a grid pattern around the tether direction
        int gridSize = 5;
        float spreadAngle = 30f; // degrees

        for (int x = -gridSize; x <= gridSize; x++)
        {
            for (int y = -gridSize; y <= gridSize; y++)
            {
                // Create a ray slightly offset from the main tether direction
                Vector3 right = Vector3.Cross(direction, Vector3.up);
                if (right.sqrMagnitude < 0.001f)
                    right = Vector3.Cross(direction, Vector3.right);
                right.Normalize();

                Vector3 up = Vector3.Cross(right, direction).normalized;

                float xOffset = (x / (float)gridSize) * Mathf.Tan(spreadAngle * Mathf.Deg2Rad);
                float yOffset = (y / (float)gridSize) * Mathf.Tan(spreadAngle * Mathf.Deg2Rad);

                Vector3 rayDir = (direction + right * xOffset + up * yOffset).normalized;
                Vector3 rayOrigin = objectCenter + rayDir * 100f;

                // Raycast back toward the object
                foreach (Collider col in colliders)
                {
                    bool wasTrigger = col.isTrigger;
                    col.isTrigger = false;

                    if (col.Raycast(new Ray(rayOrigin, -rayDir), out RaycastHit hit, 200f))
                    {
                        // Check how well this face normal aligns with the tether direction
                        float alignment = Vector3.Dot(hit.normal, direction);

                        if (alignment > bestAlignment)
                        {
                            bestAlignment = alignment;
                            bestFaceCenter = hit.point;
                            hitCount++;
                        }
                    }

                    col.isTrigger = wasTrigger;
                }
            }
        }

        if (hitCount > 0)
        {
            return bestFaceCenter;
        }

        // Fallback: if no raycast hits, use ClosestPoint directly along tether direction
        Vector3 targetPoint = objectCenter + direction * 100f;
        Vector3 closestPoint = colliders[0].ClosestPoint(targetPoint);

        foreach (Collider col in colliders)
        {
            Vector3 pointOnCollider = col.ClosestPoint(targetPoint);
            if (Vector3.Dot(pointOnCollider - objectCenter, direction) >
                Vector3.Dot(closestPoint - objectCenter, direction))
            {
                closestPoint = pointOnCollider;
            }
        }

        return closestPoint;
    }

    IEnumerator ActivateTetherAfterDelay()
    {
        tetherVisuals.SetLineColorActive();
        yield return new WaitForSeconds(activationDelay);

        ActivateJoint();
        //ActivateJoint(endJoint);

        isActivated = true;
    }
}
