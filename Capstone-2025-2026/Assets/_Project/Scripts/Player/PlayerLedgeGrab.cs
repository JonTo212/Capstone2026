using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.UI.Image;

public class PlayerLedgeGrab : MonoBehaviour
{
    [SerializeField] private Transform forwardRef;
    [SerializeField] private LayerMask grabbableLayers;
    [SerializeField] private float forwardCheckDistance = 1f;
    [SerializeField] private float verticalCheckDistance = 1f;
    [SerializeField] private float coneRadius = 0.5f;
    [SerializeField] private float coneAngle = 45f;
    [SerializeField] private float minimumGrabHeight = 1f;
    [SerializeField] private float hangDuration;
    [SerializeField] private float maxLedgeAngle = 5f;
    [SerializeField] private float ledgeJumpForceMultiplier = 1.25f;
    [SerializeField] private float maxObjectFollowSpeed;

    private CapsuleCollider _playerCol;
    private PlayerMovement _playerController;
    private float hangTimer;
    private bool canGrab;
    private Vector3 lastLedgeNormal;
    private Transform grabbedLedge;
    private Vector3 grabPosLocal;
    private Collider grabbedLedgeCollider;
    private Quaternion _localLedgeRotation;

    public bool IsHanging { get; private set; }

    private void Awake()
    {
        _playerCol = GetComponent<CapsuleCollider>();
        _playerController = GetComponent<PlayerMovement>();

        if (forwardRef == null) forwardRef = Camera.main.transform;
        canGrab = true;
    }

    private void Update()
    {
        if (IsHanging)
        {
            hangTimer += Time.deltaTime;
            bool release = grabbedLedge.TryGetComponent(out Rigidbody rb) && (rb.linearVelocity.magnitude > maxObjectFollowSpeed);
            bool tooLow = _playerController.IsGrounded();

            if (_playerController.PlayerInput.JumpDown)
            {
                HandleLedgeJump();
            }
            else if (hangTimer >= hangDuration || release || tooLow)
            {
                ReleaseLedge();
            }
            return;
        }

        var ledgePos = CheckForLedge();
        if (CanGrabLedge() && ledgePos != null)
        {
            HangOnLedge(ledgePos.Value);
            return;
        }


        if (_playerController.IsGrounded()) canGrab = true;
    }

    private void FixedUpdate()
    {
        if(IsHanging && grabbedLedge != null)
        {
            //convert relative ledge location back to world space and move rigidbody to follow it
            Vector3 worldGrabPos = grabbedLedge.TransformPoint(grabPosLocal);
            _playerController.Rb.MovePosition(worldGrabPos);

            Quaternion targetRot = grabbedLedge.rotation * _localLedgeRotation;
            _playerController.PlayerModelRotationHandler.SetNewRotationDir(targetRot);
        }
    }

    private bool CanGrabLedge()
    {
        bool notHanging = !IsHanging;

        float distToFeet = _playerCol.bounds.extents.y;
        float checkDistance = distToFeet + minimumGrabHeight;
        bool aboveMinHeight = !Physics.Raycast(transform.position, Vector3.down, checkDistance, grabbableLayers, QueryTriggerInteraction.Ignore);

        return canGrab && notHanging && aboveMinHeight;
    }

    /*private Vector3? CheckForLedge()
    {
        if (Physics.Raycast(transform.position, forwardRef.forward, out RaycastHit forwardHit, forwardCheckDistance, grabbableLayers))
        {
            float secondCheckDist = _playerCol.height;
            Vector3 secondCheckStartPos = forwardHit.point + (forwardRef.forward * _playerCol.radius) + (Vector3.up * verticalCheckDistance * secondCheckDist);

            if (Physics.Raycast(secondCheckStartPos, Vector3.down, out RaycastHit topHit, secondCheckDist, grabbableLayers))
            {
                if (topHit.collider != forwardHit.collider) return null;
                if (Mathf.Abs(Vector3.Angle(topHit.normal, Vector3.up)) > maxLedgeAngle) return null;

                lastLedgeNormal = -forwardHit.normal;
                Vector3 upOffset = Vector3.down * (_playerCol.height * verticalCheckDistance / 2f);
                Vector3 backOffset = -forwardRef.forward * _playerCol.radius * 2f;
                Vector3 target = topHit.point + backOffset + upOffset;

                return target;
            }
        }
        return null;
    }*/

    private Vector3? CheckForLedge()
    {
        if (Physics.SphereCast(transform.position, coneRadius, forwardRef.forward, out RaycastHit sphereHit, forwardCheckDistance, grabbableLayers))
        {
            Vector3 toHit = (sphereHit.point - transform.position).normalized;
            float angle = Vector3.Angle(forwardRef.forward, toHit);

            if (angle > coneAngle) //filters out things that aren't within the 'vision cone'
                return null;

            if (Physics.Raycast(sphereHit.point - forwardRef.forward * 0.1f, forwardRef.forward, out RaycastHit wallHit, 0.5f, grabbableLayers))
            {
                //raycasts from the hit point to ensure you actually hit something with depth (e.g. not a corner or something)
                return ValidateLedge(wallHit);
            }
        }


        return null;
    }

    private Vector3? ValidateLedge(RaycastHit forwardHit)
    {
        if (Vector3.Dot(forwardHit.normal, Vector3.up) > 0.5f)
        {
            //if the object that's hit is the top surface (i.e. on sloped ledges), raycast from a small buffer behind and below the hit point to get the actual wall face
            Vector3 fixOrigin = forwardHit.point - (Vector3.up * 0.1f) - (forwardRef.forward * 0.1f);

            if (Physics.Raycast(fixOrigin, forwardRef.forward, out RaycastHit sideHit, 0.5f, grabbableLayers))
            {
                forwardHit = sideHit;
            }
        }

        float secondCheckDist = _playerCol.height;

        //start 2nd check to ensure the ledge is big enough for the player to stand on
        Vector3 secondCheckStartPos =
            forwardHit.point +
            (forwardRef.forward * _playerCol.radius) +
            (Vector3.up * verticalCheckDistance * secondCheckDist);

        //raycast downward from the 2nd spot, which is above the ledge
        if (Physics.Raycast(secondCheckStartPos, Vector3.down, out RaycastHit topHit, secondCheckDist, grabbableLayers))
        {
            if (topHit.collider != forwardHit.collider) return null;
            if (Mathf.Abs(Vector3.Angle(topHit.normal, Vector3.up)) > maxLedgeAngle) return null;

            //for rotation - project the wall's face upwards to prevent rotation in weird axes
            lastLedgeNormal = Vector3.ProjectOnPlane(-forwardHit.normal, Vector3.up);
            Quaternion worldLookRot = Quaternion.LookRotation(-forwardHit.normal, topHit.normal);
            _localLedgeRotation = Quaternion.Inverse(topHit.transform.rotation) * worldLookRot;

            //desired position is 2nd spot moved backwards to accommodate for player size, then back down to accommodate for player's height
            Vector3 upOffset = Vector3.down * (_playerCol.height * verticalCheckDistance / 2f);
            Vector3 backOffset = -forwardRef.forward * _playerCol.radius * 2f;
            Vector3 target = topHit.point + backOffset + upOffset;

            //save relative location to the ledge for movement tracking
            grabbedLedge = topHit.transform;
            grabbedLedgeCollider = topHit.collider;
            grabPosLocal = grabbedLedge.InverseTransformPoint(target);

            return target;
        }

        return null;
    }

    private void HangOnLedge(Vector3 ledgePos)
    {
        _playerController.PlayerInput.ChangeSpecificInput("Move", false);
        Quaternion initialRot = grabbedLedge.rotation * _localLedgeRotation;
        _playerController.PlayerModelRotationHandler.SetNewRotationDir(initialRot);
        _playerController.Rb.position = ledgePos;
        _playerController.EnableGravity(false);
        _playerController.SetGrabbing(true);
        _playerController.Rb.linearVelocity = Vector3.zero;
        _playerController.Rb.angularVelocity = Vector3.zero;

        //disable grabbed ledge collision, otherwise there's stuttering
        if (grabbedLedgeCollider != null)
        {
            Physics.IgnoreCollision(_playerCol, grabbedLedgeCollider, true);
        }

        hangTimer = 0;
        IsHanging = true;
    }

    private void ReleaseLedge()
    {
        _playerController.PlayerInput.ChangeSpecificInput("Move", true);
        _playerController.SetGrabbing(false);
        _playerController.EnableGravity(true);
        _playerController.SetExternalForce(Vector3.zero);

        if (grabbedLedgeCollider != null)
        {
            Physics.IgnoreCollision(_playerCol, grabbedLedgeCollider, false);
            grabbedLedgeCollider = null;
        }

        IsHanging = false;
        canGrab = false;
    }

    private void HandleLedgeJump()
    {
        ReleaseLedge();
        _playerController.Jump(ledgeJumpForceMultiplier, true);
    }
}
