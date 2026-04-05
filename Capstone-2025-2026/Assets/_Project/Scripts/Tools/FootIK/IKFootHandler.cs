using UnityEngine;

public class IKFootHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask walkableLayers;

    [Header("Foot Transforms")]
    [SerializeField] private Transform leftHeel;
    [SerializeField] private Transform leftToe;
    [SerializeField] private Transform rightHeel;
    [SerializeField] private Transform rightToe;

    [Header("Foot Settings")]
    [SerializeField] private float ankleHeightOffset = 0.05f;

    [Header("Raycast Settings")]
    [SerializeField] private float raycastOriginHeight = 0.5f;
    [SerializeField] private float raycastDistance = 1.5f;

    [Header("Smoothing")]
    [SerializeField] private float footPositionSmoothTime = 0.08f;
    [SerializeField] private float footRotationSpeed = 10f;
    [SerializeField] private float hipPositionSmoothTime = 0.12f;

    [Header("IK Weight")]
    [Tooltip("Name of the float curve in your animation clips that drives left foot planting (0=swing, 1=planted). Standard Unity humanoid clips use LeftFoot.")]
    [SerializeField] private string leftFootCurveName = "LeftFoot";
    [Tooltip("Name of the float curve in your animation clips that drives right foot planting (0=swing, 1=planted). Standard Unity humanoid clips use RightFoot.")]
    [SerializeField] private string rightFootCurveName = "RightFoot";
    [Tooltip("Speed at which IK weight fades in/out. Higher = snappier.")]
    [SerializeField] private float ikWeightSpeed = 15f;

    // Per-foot persistent smoothed positions (fixes re-anchoring bug)
    private Vector3 leftSmoothedPos;
    private Vector3 rightSmoothedPos;

    // Per-foot smoothdamp velocities
    private Vector3 leftPosSmoothVel;
    private Vector3 rightPosSmoothVel;

    // Per-foot smoothed rotations
    private Quaternion leftSmoothedRot;
    private Quaternion rightSmoothedRot;

    // Per-foot IK weights (fade to 0 when foot lifts off)
    private float leftIKWeight = 1f;
    private float rightIKWeight = 1f;

    // Hip smoothing
    private float hipOffsetVel;
    private float currentHipOffset;

    // Full 3D IK foot target positions written by SolveFoot, read by SolveHipHeight
    private Vector3 leftFootTargetPos;
    private Vector3 rightFootTargetPos;

    // Whether each foot has a valid ground hit this frame -- gates the hip solver
    private bool leftFootGrounded;
    private bool rightFootGrounded;

    // Leg chain lengths measured once in Awake
    private float leftLegLength;
    private float rightLegLength;

    // Body position last frame -- used to carry smoothed foot positions with locomotion
    private Vector3 lastBodyPosition;


    private void Awake()
    {
        leftSmoothedRot = animator.GetBoneTransform(HumanBodyBones.LeftFoot).rotation;
        rightSmoothedRot = animator.GetBoneTransform(HumanBodyBones.RightFoot).rotation;

        leftSmoothedPos = animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
        rightSmoothedPos = animator.GetBoneTransform(HumanBodyBones.RightFoot).position;

        leftFootTargetPos = leftSmoothedPos;
        rightFootTargetPos = rightSmoothedPos;

        leftLegLength = MeasureLegLength(HumanBodyBones.LeftFoot);
        rightLegLength = MeasureLegLength(HumanBodyBones.RightFoot);

        lastBodyPosition = animator.bodyPosition;
    }

    private float MeasureLegLength(HumanBodyBones foot)
    {
        Transform footT = animator.GetBoneTransform(foot);
        Transform lower = animator.GetBoneTransform(
            foot == HumanBodyBones.LeftFoot ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg);
        Transform upper = animator.GetBoneTransform(
            foot == HumanBodyBones.LeftFoot ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg);

        if (footT == null || lower == null || upper == null) return 1f;

        return Vector3.Distance(upper.position, lower.position)
             + Vector3.Distance(lower.position, footT.position);
    }

    private void OnAnimatorIK(int layerIndex)
    {
        // Capture base body position BEFORE any modification to avoid compounding offset each frame
        Vector3 baseBodyPos = animator.bodyPosition;

        // Carry smoothed foot positions with the body so locomotion doesn't cause lag
        Vector3 bodyDelta = baseBodyPos - lastBodyPosition;
        leftSmoothedPos += bodyDelta;
        rightSmoothedPos += bodyDelta;
        lastBodyPosition = baseBodyPos;

        // Solve feet -- weights and targets are set inside SolveFoot
        SolveFoot(AvatarIKGoal.LeftFoot, HumanBodyBones.LeftFoot, leftHeel, leftToe, ref leftSmoothedPos, ref leftPosSmoothVel, ref leftSmoothedRot, ref leftFootTargetPos, ref leftIKWeight, ref leftFootGrounded);
        SolveFoot(AvatarIKGoal.RightFoot, HumanBodyBones.RightFoot, rightHeel, rightToe, ref rightSmoothedPos, ref rightPosSmoothVel, ref rightSmoothedRot, ref rightFootTargetPos, ref rightIKWeight, ref rightFootGrounded);

        // Push hips down so the lower foot can reach the ground
        SolveHipHeight(baseBodyPos);
    }

    private void SolveFoot(AvatarIKGoal goal, HumanBodyBones footBone,
                            Transform heelTransform, Transform toeTransform,
                            ref Vector3 smoothedPos, ref Vector3 posSmoothVel,
                            ref Quaternion smoothedRot,
                            ref Vector3 footTargetPos,
                            ref float ikWeight,
                            ref bool footGrounded)
    {
        HumanBodyBones kneeBone = (footBone == HumanBodyBones.LeftFoot)
            ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg;

        HumanBodyBones hipBone = (footBone == HumanBodyBones.LeftFoot)
            ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg;

        Transform boneTransform = animator.GetBoneTransform(footBone);
        Transform kneeTransform = animator.GetBoneTransform(kneeBone);
        Transform hipTransform = animator.GetBoneTransform(hipBone);
        if (boneTransform == null || kneeTransform == null) return;
        if (heelTransform == null || toeTransform == null) return;

        Vector3 heelWorld = heelTransform.position;
        Vector3 toeWorld = toeTransform.position;

        // --- Primary position cast: project foot XZ under the knee Y so a forward-facing
        //     step is detected even when heel and toe both sit on the same flat surface.
        Vector3 kneeProjectedOrigin = new Vector3(boneTransform.position.x,
                                                   kneeTransform.position.y,
                                                   boneTransform.position.z);
        bool kneeCastHit = CheckGroundedFrom(kneeProjectedOrigin, out RaycastHit kneeCastInfo);

        // --- Secondary heel / toe casts for rotation
        bool heelHit = CheckGrounded(heelWorld, out RaycastHit heelInfo);
        bool toeHit = CheckGrounded(toeWorld, out RaycastHit toeInfo);
        bool footObstructed = CheckBetweenToeAndHeel(heelWorld, toeWorld);

        if (footObstructed)
        {
            bool innerHeelHit = CheckGrounded(Vector3.Lerp(heelWorld, toeWorld, 0.25f), out RaycastHit innerHeelInfo);
            bool innerToeHit = CheckGrounded(Vector3.Lerp(heelWorld, toeWorld, 0.75f), out RaycastHit innerToeInfo);

            if (innerHeelHit) { heelInfo = innerHeelInfo; heelHit = true; toeHit = false; }
            else if (innerToeHit) { toeInfo = innerToeInfo; toeHit = true; heelHit = false; }
        }

        // Track whether any cast found ground this frame so SolveHipHeight can skip this leg
        footGrounded = kneeCastHit || heelHit || toeHit;

        // --- IK weight: read the animation clip's foot planting curve (0=swing, 1=planted).
        //     This is the same signal Unity's built-in foot IK uses, so it matches the
        //     animation exactly regardless of speed or clip.
        //     Falls back to 1 (always planted) if the curve doesn't exist in the clip.
        //     When no ground is found (hanging, airborne) we force target to 0 regardless
        //     of the curve so IK never applies a rotation derived from a missing surface.
        string curveName = (footBone == HumanBodyBones.LeftFoot) ? leftFootCurveName : rightFootCurveName;
        float targetWeight = footGrounded ? Mathf.Clamp01(animator.GetFloat(curveName)) : 0f;
        ikWeight = Mathf.MoveTowards(ikWeight, targetWeight, Time.deltaTime * ikWeightSpeed);

        animator.SetIKPositionWeight(goal, ikWeight);
        animator.SetIKRotationWeight(goal, ikWeight);

        // When IK is fully off, snap smoothedPos to the animated bone so there
        // is no pop when the foot lands and IK fades back in.
        if (ikWeight <= 0f)
        {
            smoothedPos = boneTransform.position;
            posSmoothVel = Vector3.zero;
            smoothedRot = boneTransform.rotation;
            // Park footTargetPos at the hip so the hip solver sees zero required drop
            footTargetPos = hipTransform != null ? hipTransform.position : boneTransform.position;
            return;
        }

        // --- Position: prefer knee-projected hit so forward-facing steps work,
        //     fall back to heel/toe hits, then animated bone.
        Vector3 desiredPos;
        if (kneeCastHit)
            desiredPos = kneeCastInfo.point + kneeCastInfo.normal * ankleHeightOffset;
        else if (heelHit)
            desiredPos = heelInfo.point + heelInfo.normal * ankleHeightOffset;
        else if (toeHit)
            desiredPos = toeInfo.point - (toeWorld - heelWorld).normalized * Vector3.Distance(heelWorld, toeWorld)
                         + toeInfo.normal * ankleHeightOffset;
        else
            desiredPos = boneTransform.position;

        // Smooth from the persistent smoothedPos rather than boneTransform.position each frame
        smoothedPos = Vector3.SmoothDamp(smoothedPos, desiredPos, ref posSmoothVel, footPositionSmoothTime);
        animator.SetIKPosition(goal, smoothedPos);

        // Only update footTargetPos from a real ground hit so the hip solver never chases
        // a phantom position while the character is hanging, jumping, or airborne.
        if (footGrounded)
            footTargetPos = smoothedPos;
        else if (hipTransform != null)
            footTargetPos = hipTransform.position; // zero-drop sentinel: leg already "reaches" its own hip

        // --- Rotation: use heel+toe for best accuracy when available,
        //     fall back to knee-cast normal, then slope from whichever single cast hit.
        Quaternion desiredRot;
        if (heelHit && toeHit)
        {
            Vector3 heelToToe = (toeInfo.point - heelInfo.point).normalized;
            Vector3 footUp = heelInfo.normal;
            Vector3 footRight = Vector3.Cross(footUp, heelToToe).normalized;
            Vector3 footForward = Vector3.Cross(footRight, footUp).normalized;
            desiredRot = Quaternion.LookRotation(footForward, footUp);
        }
        else if (kneeCastHit)
        {
            Quaternion slopeRot = Quaternion.FromToRotation(Vector3.up, kneeCastInfo.normal);
            desiredRot = Quaternion.LookRotation(slopeRot * transform.forward, slopeRot * Vector3.up);
        }
        else if (heelHit)
        {
            Quaternion slopeRot = Quaternion.FromToRotation(Vector3.up, heelInfo.normal);
            desiredRot = Quaternion.LookRotation(slopeRot * transform.forward, slopeRot * Vector3.up);
        }
        else if (toeHit)
        {
            Quaternion slopeRot = Quaternion.FromToRotation(Vector3.up, toeInfo.normal);
            desiredRot = Quaternion.LookRotation(slopeRot * transform.forward, slopeRot * Vector3.up);
        }
        else
        {
            // No ground found (hanging, airborne, etc.).
            // IK weight is already fading to 0 above, so this rotation won't be visible for long.
            // Slerp smoothedRot back toward the raw animated bone rotation so it's in a clean
            // state when the foot lands and IK fades back in -- avoids the corrupt-freeze problem.
            smoothedRot = Quaternion.Slerp(smoothedRot, boneTransform.rotation, Time.deltaTime * footRotationSpeed);
            animator.SetIKRotation(goal, smoothedRot);
            return;
        }

        smoothedRot = Quaternion.Slerp(smoothedRot, desiredRot, Time.deltaTime * footRotationSpeed);
        animator.SetIKRotation(goal, smoothedRot);
    }

    private void SolveHipHeight(Vector3 baseBodyPos)
    {
        Transform leftHip = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        Transform rightHip = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        if (leftHip == null || rightHip == null) return;

        // If neither foot has a ground hit (hanging, jumping, airborne) smoothly
        // return the hip offset to zero and bail out -- no drop calculation needed.
        if (!leftFootGrounded && !rightFootGrounded)
        {
            currentHipOffset = Mathf.SmoothDamp(currentHipOffset, 0f, ref hipOffsetVel, hipPositionSmoothTime);
            animator.bodyPosition = baseBodyPos + Vector3.up * currentHipOffset;
            return;
        }

        // Preserve the hip positions relative to body so lateral offset stays correct
        Vector3 leftHipOffset = leftHip.position - animator.bodyPosition;
        Vector3 rightHipOffset = rightHip.position - animator.bodyPosition;

        // Iteratively find the body Y that satisfies both legs (3 passes converges well).
        // Ungrounded legs contribute 0 drop so they never skew the result.
        float targetBodyY = baseBodyPos.y;

        for (int i = 0; i < 3; i++)
        {
            Vector3 candidateBody = new Vector3(baseBodyPos.x, targetBodyY, baseBodyPos.z);
            Vector3 lHipPos = candidateBody + leftHipOffset;
            Vector3 rHipPos = candidateBody + rightHipOffset;

            float leftDrop = leftFootGrounded ? ComputeRequiredBodyDrop(lHipPos, leftFootTargetPos, leftLegLength) : 0f;
            float rightDrop = rightFootGrounded ? ComputeRequiredBodyDrop(rHipPos, rightFootTargetPos, rightLegLength) : 0f;

            float totalDrop = Mathf.Min(leftDrop, rightDrop, 0f);
            if (Mathf.Abs(totalDrop) < 0.001f) break;
            targetBodyY += totalDrop;
        }

        float targetOffset = targetBodyY - baseBodyPos.y;

        currentHipOffset = Mathf.SmoothDamp(currentHipOffset, targetOffset, ref hipOffsetVel, hipPositionSmoothTime);

        // Apply to the captured base position so the offset never compounds across frames
        animator.bodyPosition = baseBodyPos + Vector3.up * currentHipOffset;
    }

    // Returns how far the body must drop (negative value) so the hip can reach footTarget.
    // Returns 0 if the leg already reaches.
    private float ComputeRequiredBodyDrop(Vector3 hipPos, Vector3 footTarget, float legLength)
    {
        float horizontalDist = Vector3.Distance(
            new Vector3(hipPos.x, 0f, hipPos.z),
            new Vector3(footTarget.x, 0f, footTarget.z));

        if (horizontalDist >= legLength) return 0f;

        float maxVertical = Mathf.Sqrt(legLength * legLength - horizontalDist * horizontalDist);
        float requiredVertical = hipPos.y - footTarget.y;

        if (requiredVertical <= maxVertical) return 0f;

        return maxVertical - requiredVertical;
    }

    private bool CheckGrounded(Vector3 origin, out RaycastHit hit)
    {
        Vector3 rayOrigin = origin + Vector3.up * raycastOriginHeight;
        return Physics.Raycast(rayOrigin, Vector3.down, out hit, raycastOriginHeight + raycastDistance, walkableLayers);
    }

    // Cast downward from an already-elevated origin (e.g. knee-height) without adding raycastOriginHeight again
    private bool CheckGroundedFrom(Vector3 elevatedOrigin, out RaycastHit hit)
    {
        return Physics.Raycast(elevatedOrigin, Vector3.down, out hit, raycastDistance * 2f, walkableLayers);
    }

    private bool CheckBetweenToeAndHeel(Vector3 heelPos, Vector3 toePos)
    {
        Vector3 toeToHeel = heelPos - toePos;
        float footLength = toeToHeel.magnitude;
        return Physics.Raycast(toePos, toeToHeel.normalized, out RaycastHit _, footLength, walkableLayers);
    }

    private void OnDrawGizmos()
    {
        if (animator == null) return;

        DrawFootGizmos(HumanBodyBones.LeftFoot, leftHeel, leftToe, leftFootTargetPos, Color.cyan, Color.blue);
        DrawFootGizmos(HumanBodyBones.RightFoot, rightHeel, rightToe, rightFootTargetPos, Color.yellow, Color.red);

        Transform lHip = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        Transform rHip = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        Gizmos.color = Color.magenta;
        if (lHip != null) Gizmos.DrawWireSphere(lHip.position, 0.07f);
        if (rHip != null) Gizmos.DrawWireSphere(rHip.position, 0.07f);
        Gizmos.DrawWireSphere(animator.bodyPosition, 0.09f);
    }

    private void DrawFootGizmos(HumanBodyBones footBone, Transform heel, Transform toe,
                                  Vector3 footTargetPos, Color rayColor, Color hitColor)
    {
        if (heel == null || toe == null) return;
        Transform bone = animator.GetBoneTransform(footBone);

        Vector3 heelOrigin = heel.position + Vector3.up * raycastOriginHeight;
        Vector3 toeOrigin = toe.position + Vector3.up * raycastOriginHeight;

        Gizmos.color = rayColor;
        Gizmos.DrawLine(heelOrigin, heelOrigin + Vector3.down * (raycastOriginHeight + raycastDistance));
        Gizmos.DrawLine(toeOrigin, toeOrigin + Vector3.down * (raycastOriginHeight + raycastDistance));

        if (CheckGrounded(heel.position, out RaycastHit heelHit))
        {
            Gizmos.color = hitColor;
            Gizmos.DrawWireSphere(heelHit.point, 0.04f);
            Gizmos.DrawLine(heelHit.point, heelHit.point + heelHit.normal * 0.15f);
        }
        if (CheckGrounded(toe.position, out RaycastHit toeHit))
        {
            Gizmos.color = hitColor;
            Gizmos.DrawWireSphere(toeHit.point, 0.04f);
            Gizmos.DrawLine(toeHit.point, toeHit.point + toeHit.normal * 0.15f);
        }

        if (bone != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(bone.position, 0.05f);
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(footTargetPos, 0.06f);
    }
}