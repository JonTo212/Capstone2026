using FMODUnity;
using System;
using System.Collections;
using UnityEngine;

public class PlayerLedgeGrab : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform forwardRef; //camera transform, used as directional fallback when there's no input
    [SerializeField] private LayerMask grabbableLayers;

    [Header("Detection")]
    [SerializeField] private float forwardCheckDistance = 1f;
    [SerializeField] private float upwardCheckDistance = 1.2f;  //how far the upward sweep reaches when jumping into a ledge from below
    [SerializeField] private float verticalCheckDistance = 1f;  //raycast range multiplier for the top-surface downcast
    [SerializeField] private float coneAngle = 30f;             //max angle between cast direction and hit - tighter = fewer false positives
    [SerializeField] private float minimumGrabHeight = 1f;
    [SerializeField] private float maxLedgeAngle = 5f;

    [Header("Hang Position")]
    [SerializeField] private float hangWallStandoff = 0.15f;    //how far from the wall face the capsule centre sits while hanging, additive on top of radius

    [Header("Mantle End Position")]
    [SerializeField] private float mantleForwardDistance = 0.3f; //how far past the ledge edge the capsule centre lands after climbing, tune to match animation foot placement

    [Header("Hang Settings")]
    [SerializeField] private float hangDuration;
    [SerializeField] private float ledgeJumpForceMultiplier = 1.25f;
    [SerializeField] private float maxObjectFollowSpeed;

    [Header("Grab Cooldown")]
    [SerializeField] private float grabCooldownTime = 0.3f;

    private CapsuleCollider _playerCol;
    private float hangTimer;
    private float _grabCooldown;
    private Transform grabbedLedge;
    private Vector3 grabPosLocal;
    private Collider grabbedLedgeCollider;
    private Quaternion _localLedgeRotation;
    private Vector3 _grabWallNormal;
    public event Action<bool> OnMantle;

    public bool IsHanging { get; private set; }

    private void Awake()
    {
        _playerCol = GetComponent<CapsuleCollider>();

        if (forwardRef == null) forwardRef = Camera.main.transform;
    }

    private void Update()
    {
        if (IsHanging)
        {
            hangTimer += Time.deltaTime;
            bool release = grabbedLedge.TryGetComponent(out Rigidbody rb) && rb.linearVelocity.magnitude > maxObjectFollowSpeed;
            bool tooLow = PlayerRefData.Instance.PlayerMovement.IsGrounded();

            if (PlayerActions.Instance.JumpDown)
            {
                HandleLedgeJump();
            }
            else if (PlayerActions.Instance.MoveInput.y > 0.01f)
            {
                ClimbOnLedge(CalculateMantleTarget());
            }
            else if (hangTimer >= hangDuration || release || tooLow)
            {
                ReleaseLedge();
            }
            return;
        }

        if (_grabCooldown > 0f)
            _grabCooldown -= Time.deltaTime;

        if (CanGrabLedge())
        {
            var ledgePos = CheckForLedge();
            if (ledgePos != null)
                HangOnLedge(ledgePos.Value);
        }
    }

    private void FixedUpdate()
    {
        if (IsHanging && grabbedLedge != null && _mantleCoroutine == null)
        {
            //convert relative ledge location back to world space and move rigidbody to follow it
            Vector3 worldGrabPos = grabbedLedge.TransformPoint(grabPosLocal);
            PlayerRefData.Instance.PlayerMovement.Rb.MovePosition(worldGrabPos);

            Quaternion targetRot = grabbedLedge.rotation * _localLedgeRotation;
            PlayerRefData.Instance.PlayerModelRotationHandler.SetNewRotationDir(targetRot, true);
        }
    }

    private bool CanGrabLedge()
    {
        if (IsHanging) return false;
        if (_grabCooldown > 0f) return false;

        //prevent grabbing ledge faces while walking past them at foot level
        float distToFeet = _playerCol.bounds.extents.y;
        bool aboveMinHeight = !Physics.Raycast(transform.position, Vector3.down, distToFeet + minimumGrabHeight, grabbableLayers, QueryTriggerInteraction.Ignore);

        return aboveMinHeight;
    }

    private Vector3? CheckForLedge()
    {
        //use WishDir when input exists, fall back to camera forward so walking up to a stationary ledge still works
        Vector3 wishDir = PlayerRefData.Instance.PlayerMovement.WishDir;
        bool hasInput = wishDir.sqrMagnitude > 0.001f;

        Vector3 horizontalDir = hasInput
            ? wishDir
            : new Vector3(forwardRef.forward.x, 0f, forwardRef.forward.z).normalized;

        //cast 1 - horizontal: sweeps forward at upper-chest height, aligned to where wall faces live
        Vector3 horizontalOrigin = transform.position + Vector3.up * (_playerCol.height * 0.5f - _playerCol.radius);
        Vector3? result = TrySweepAndValidate(horizontalOrigin, _playerCol.radius, horizontalDir, forwardCheckDistance);
        if (result != null) return result;

        //cast 2 - upward: the horizontal cast misses ledge overhangs directly above the player, this catches them
        bool movingUp = PlayerRefData.Instance.PlayerMovement.Rb.linearVelocity.y > 0.5f;
        if (movingUp)
        {
            //origin at shoulder height so the sphere has room to sweep before exiting the top of the capsule
            Vector3 upwardOrigin = transform.position + Vector3.up * (_playerCol.height * 0.25f);
            result = TrySweepAndValidate(upwardOrigin, _playerCol.radius, Vector3.up, upwardCheckDistance);
            if (result != null) return result;
        }

        return null;
    }

    //shared sweep-and-validate logic used by both cast paths
    private Vector3? TrySweepAndValidate(Vector3 origin, float radius, Vector3 direction, float distance)
    {
        if (!Physics.SphereCast(origin, radius, direction, out RaycastHit sphereHit, distance, grabbableLayers, QueryTriggerInteraction.Ignore))
            return null;

        //filters out floors and side walls the sphere clips incidentally
        Vector3 toHit = (sphereHit.point - origin).normalized;
        if (Vector3.Angle(direction, toHit) > coneAngle)
            return null;

        //re-cast with a ray from just behind the hit to get a precise wall normal, spherecast centroids near edges can be unreliable
        Vector3 rayOrigin = sphereHit.point - direction * 0.1f;
        if (!Physics.Raycast(rayOrigin, direction, out RaycastHit wallHit, 0.3f, grabbableLayers, QueryTriggerInteraction.Ignore))
            return null;

        return ValidateLedge(wallHit, direction);
    }

    private Vector3? ValidateLedge(RaycastHit forwardHit, Vector3 approachDir)
    {
        //if the hit landed on a top surface rather than the wall face, re-cast from just below to find the true wall normal
        if (Vector3.Angle(forwardHit.normal, Vector3.up) < maxLedgeAngle)
        {
            Vector3 fixOrigin = forwardHit.point - (Vector3.up * 0.1f) - (approachDir * 0.1f);
            if (Physics.Raycast(fixOrigin, approachDir, out RaycastHit sideHit, 0.5f, grabbableLayers, QueryTriggerInteraction.Ignore))
                forwardHit = sideHit;
        }

        //start 2nd check to ensure the ledge is big enough for the player to stand on
        Vector3 secondCheckStartPos = forwardHit.point + approachDir * _playerCol.radius + Vector3.up * verticalCheckDistance * _playerCol.height;

        //raycast downward from the 2nd spot, which is above the ledge
        if (!Physics.Raycast(secondCheckStartPos, Vector3.down, out RaycastHit topHit, _playerCol.height, grabbableLayers))
            return null;

        //compare gameobjects instead of colliders, to ensure we don't get blocked by separate colliders on the same object (e.g. a rock with separate mesh colliders for walls and top)
        if (topHit.collider.gameObject != forwardHit.collider.gameObject) return null;

        //use averaged normals instead of the raw hit normal, which is per-triangle on mesh colliders and can be wildly inaccurate on low-poly or uneven geometry
        Vector3 robustTopNormal = GetRobustNormal(topHit);
        if (Mathf.Abs(Vector3.Angle(robustTopNormal, Vector3.up)) > maxLedgeAngle) return null;

        //for rotation - project the wall's face upwards to prevent rotation in weird axes
        Quaternion worldLookRot = Quaternion.LookRotation(-forwardHit.normal, robustTopNormal);
        _localLedgeRotation = Quaternion.Inverse(topHit.transform.rotation) * worldLookRot;

        Vector3 wallBack = new Vector3(forwardHit.normal.x, 0f, forwardHit.normal.z).normalized;
        _grabWallNormal = wallBack; //bake for use in CalculateMantleTarget and GetRobustWallNormal

        //hang position: capsule top flush with ledge surface, offset from wall by radius + standoff so the capsule doesn't clip the face
        Vector3 upOffset = Vector3.down * (_playerCol.height * 0.5f);
        Vector3 backOffset = wallBack * (_playerCol.radius + hangWallStandoff);
        Vector3 target = topHit.point + backOffset + upOffset;

        //snap Y precisely to the surface
        Vector3 snapOrigin = new Vector3(target.x, topHit.point.y + _playerCol.height, target.z);
        if (Physics.Raycast(snapOrigin, Vector3.down, out RaycastHit surfaceCheck, _playerCol.height * 2f, grabbableLayers, QueryTriggerInteraction.Ignore))
            target.y = surfaceCheck.point.y + _playerCol.height * 0.5f;

        //save relative location to the ledge for movement tracking
        grabbedLedge = topHit.transform;
        grabbedLedgeCollider = topHit.collider;
        grabPosLocal = grabbedLedge.InverseTransformPoint(target);

        return target;
    }

    //samples the surface normal at several nearby points and averages them
    //smooths out per-triangle noise from mesh colliders, giving a more stable normal for the angle check and rotation calculation
    private Vector3 GetRobustNormal(RaycastHit hit)
    {
        Vector3 n = hit.normal;
        Vector3 tangent = Vector3.Cross(n, Vector3.up);
        if (tangent.sqrMagnitude < 0.01f) tangent = Vector3.Cross(n, Vector3.right);
        tangent.Normalize();

        float offset = 0.05f;
        Vector3 p = hit.point + n * 0.02f; //lift slightly off surface to avoid self-intersection
        Vector3 avg = n;
        int count = 1;

        for (int i = 0; i < 3; i++)
        {
            float angle = i * 120f * Mathf.Deg2Rad;
            Vector3 bitangent = Vector3.Cross(n, tangent);
            Vector3 sampleOffset = (Mathf.Cos(angle) * tangent + Mathf.Sin(angle) * bitangent) * offset;
            if (Physics.Raycast(p + sampleOffset, -n, out RaycastHit sampleHit, 0.15f, grabbableLayers, QueryTriggerInteraction.Ignore))
            {
                avg += sampleHit.normal;
                count++;
            }
        }

        return (avg / count).normalized;
    }

    private void HangOnLedge(Vector3 ledgePos)
    {
        PlayerRefData.Instance.PlayerMovement.Rb.MovePosition(ledgePos);
        PlayerRefData.Instance.PlayerMovement.EnableGravity(false);
        PlayerRefData.Instance.PlayerMovement.SetGrabbing(true);
        PlayerRefData.Instance.PlayerMovement.KillVelocity();
        PlayerRefData.Instance.LassoTetherController.SetLassoState(false);
        PlayerRefData.Instance.LassoTetherController.SetTetherState(false);

        //disable grabbed ledge collision, otherwise there's stuttering
        if (grabbedLedgeCollider != null)
            Physics.IgnoreCollision(_playerCol, grabbedLedgeCollider, true);

        hangTimer = 0;
        IsHanging = true;
        RuntimeManager.PlayOneShot("event:/Mantle", transform.position);
    }

    private void ReleaseLedge(bool restoreCollision = true)
    {
        PlayerRefData.Instance.PlayerModelRotationHandler.SetNewRotationDir(null, false);
        PlayerRefData.Instance.PlayerMovement.SetGrabbing(false);
        PlayerRefData.Instance.PlayerMovement.EnableGravity(true);
        PlayerRefData.Instance.PlayerMovement.SetExternalForce(Vector3.zero);
        PlayerRefData.Instance.LassoTetherController.SetLassoState(true);
        PlayerRefData.Instance.LassoTetherController.SetTetherState(true);

        if (restoreCollision && grabbedLedgeCollider != null)
        {
            Physics.IgnoreCollision(_playerCol, grabbedLedgeCollider, false);
            grabbedLedgeCollider = null;
        }

        IsHanging = false;
        _grabCooldown = grabCooldownTime;
    }

    private void HandleLedgeJump()
    {
        PlayerRefData.Instance.PlayerMovement.Rb.isKinematic = false;
        PlayerRefData.Instance.PlayerMovement.DisableJump(true);
        ReleaseLedge();
        PlayerRefData.Instance.PlayerMovement.Jump(ledgeJumpForceMultiplier, true);
        PlayerRefData.Instance.PlayerMovement.DisableJump(false);
        RuntimeManager.PlayOneShot("event:/Jump", transform.position);
    }

    private void ClimbOnLedge(Vector3 ledgePos)
    {
        if (_mantleCoroutine != null) return;
        RuntimeManager.PlayOneShot("event:/Mantle", transform.position);
        _mantleCoroutine = StartCoroutine(Mantle(ledgePos));
    }

    public float mantleDuration = 0.45f;
    public Coroutine _mantleCoroutine;

    private IEnumerator Mantle(Vector3 targetPos)
    {
        PlayerRefData.Instance.PlayerMovement.Rb.isKinematic = true;
        OnMantle?.Invoke(true);

        //snap to authoritative hang position before starting the arc
        Vector3 worldGrabPos = grabbedLedge.TransformPoint(grabPosLocal);
        PlayerRefData.Instance.PlayerMovement.Rb.MovePosition(worldGrabPos);

        Quaternion targetRot = grabbedLedge.rotation * _localLedgeRotation;
        PlayerRefData.Instance.PlayerModelRotationHandler.SetNewRotationDir(targetRot, true);

        Vector3 p0 = PlayerRefData.Instance.PlayerMovement.Rb.position;
        targetPos = SafeMantleTarget(targetPos);
        Vector3 p3 = targetPos;

        //re-sample wall normal at mantle time - the value baked at grab time can be from a badly-angled triangle and misalign the arc
        Vector3 freshWallNormal = GetRobustWallNormal(p0, p3);
        float heightDiff = p3.y - p0.y;

        //p1: push back along the wall normal so the arc hugs the rock face on the way up
        Vector3 p1 = p0 + freshWallNormal * _playerCol.radius;
        p1.y = p0.y + heightDiff * 0.8f;

        //p2: pull back slightly from target so the arc arrives flat rather than still moving forward
        Vector3 p2 = p3 + freshWallNormal * _playerCol.radius;
        p2.y = p3.y;

        float timer = 0f;
        while (timer < mantleDuration)
        {
            if (PlayerActions.Instance.JumpDown)
            {
                HandleLedgeJump();
                _mantleCoroutine = null;
                yield break;
            }

            timer += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(timer / mantleDuration);
            float et = t * t * (3f - 2f * t); //smooth-step ease

            //cubic bezier: B(t) = (1-t)³P0 + 3(1-t)²tP1 + 3(1-t)t²P2 + t³P3
            float u = 1f - et;
            Vector3 pos = (u * u * u) * p0
                        + (3f * u * u * et) * p1
                        + (3f * u * et * et) * p2
                        + (et * et * et) * p3;

            PlayerRefData.Instance.PlayerMovement.Rb.MovePosition(pos);
            yield return new WaitForFixedUpdate();
        }

        //settle position before re-enabling collision so the capsule is fully placed before physics reacts
        PlayerRefData.Instance.PlayerMovement.Rb.position = targetPos;
        PlayerRefData.Instance.PlayerMovement.Rb.isKinematic = false;

        if (grabbedLedgeCollider != null)
        {
            Physics.IgnoreCollision(_playerCol, grabbedLedgeCollider, false);
            grabbedLedgeCollider = null;
        }

        ReleaseLedge(restoreCollision: false);
        _mantleCoroutine = null;
    }

    private Vector3 CalculateMantleTarget()
    {
        //authoritative hang position in world space
        Vector3 hangPos = grabbedLedge.TransformPoint(grabPosLocal);

        //negate wall normal to point into the platform, offset by radius + forward distance
        Vector3 ontoLedge = -_grabWallNormal * (_playerCol.radius + mantleForwardDistance);

        Vector3 target = hangPos + ontoLedge;
        target.y = hangPos.y + (_playerCol.height * 0.5f); //SafeMantleTarget corrects this against actual geometry

        return target;
    }

    //snaps the candidate Y to the actual surface so the capsule stands on geometry rather than floating or clipping
    private Vector3 SafeMantleTarget(Vector3 candidate)
    {
        Vector3 castOrigin = new Vector3(candidate.x, candidate.y + _playerCol.height, candidate.z);

        if (Physics.Raycast(castOrigin, Vector3.down, out RaycastHit hit, _playerCol.height * 1.5f, grabbableLayers, QueryTriggerInteraction.Ignore))
            candidate.y = hit.point.y + _playerCol.height * 0.5f;

        return candidate;
    }

    //fires raycasts toward the wall at multiple heights and averages the wall-facing normals, more stable than a single ray on coarse mesh colliders
    private Vector3 GetRobustWallNormal(Vector3 playerPos, Vector3 targetPos)
    {
        Vector3 toTarget = targetPos - playerPos;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.001f) return _grabWallNormal;
        Vector3 wallDir = toTarget.normalized;

        Vector3 avg = Vector3.zero;
        int count = 0;

        float[] heights = { 0f, _playerCol.height * 0.35f, _playerCol.height * 0.7f };
        foreach (float h in heights)
        {
            Vector3 origin = playerPos + Vector3.up * h;
            if (Physics.Raycast(origin, wallDir, out RaycastHit hit, forwardCheckDistance * 1.5f, grabbableLayers, QueryTriggerInteraction.Ignore))
            {
                //ignore normals too close to vertical - those are top-surface bleed at the edge, not the wall face
                if (Mathf.Abs(Vector3.Dot(hit.normal, Vector3.up)) < 0.5f)
                {
                    avg += hit.normal;
                    count++;
                }
            }
        }

        if (count == 0) return _grabWallNormal;

        //flatten to horizontal, we only want XZ orientation for the arc direction
        Vector3 result = avg / count;
        result.y = 0f;
        return result.sqrMagnitude > 0.001f ? result.normalized : _grabWallNormal;
    }
}