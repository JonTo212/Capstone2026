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

    private CapsuleCollider _playerCol;
    private PlayerMovement _playerController;
    private float hangTimer;
    private bool canGrab;
    private Vector3 lastLedgeNormal;

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
        var ledgePos = CheckForLedge();
        if (CanGrabLedge())
        {
            HangOnLedge(ledgePos.Value);
            return;
        }

        if(IsHanging)
        {
            hangTimer += Time.deltaTime;
            if (_playerController.PlayerInput.JumpDown)
            {
                HandleLedgeJump();
            }
            else if (hangTimer >= hangDuration)
            {
                ReleaseLedge();
            }
        }

        if (_playerController.IsGrounded()) canGrab = true;
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

            if (angle > coneAngle)
                return null;

            if (Physics.Raycast(sphereHit.point - forwardRef.forward * 0.1f, forwardRef.forward, out RaycastHit wallHit, 0.5f, grabbableLayers))
            {
                return ValidateLedge(wallHit);
            }
        }


        return null;
    }

    private Vector3? ValidateLedge(RaycastHit forwardHit)
    {
        float secondCheckDist = _playerCol.height;

        Vector3 secondCheckStartPos =
            forwardHit.point +
            (forwardRef.forward * _playerCol.radius) +
            (Vector3.up * verticalCheckDistance * secondCheckDist);

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

        return null;
    }

    private void HangOnLedge(Vector3 ledgePos)
    {
        _playerController.PlayerInput.ChangeSpecificInput("Move", false);
        _playerController.PlayerModelRotationHandler.SetNewRotationDir(lastLedgeNormal, hangDuration);
        _playerController.Rb.isKinematic = true;
        _playerController.Rb.MovePosition(ledgePos);
        _playerController.SetGrabbing(true);

        hangTimer = 0;
        IsHanging = true;
    }

    private void ReleaseLedge()
    {
        _playerController.PlayerInput.ChangeSpecificInput("Move", true);
        _playerController.SetGrabbing(false);
        _playerController.Rb.isKinematic = false;
        IsHanging = false;
        canGrab = false;
    }

    private void HandleLedgeJump()
    {
        ReleaseLedge();
        _playerController.Jump();
        _playerController.PlayerModelRotationHandler.SetNewRotationDir(null, 0f);
    }
}
