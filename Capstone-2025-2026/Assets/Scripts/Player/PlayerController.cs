using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerMovementState
{
    Walking,
    Running,
    InAir
}

public enum PlayerStanceState
{
    Standing,
    Crouching
}

public class PlayerController : MonoBehaviour
{
    [Header("Movement variables")]
    private Vector3 wishDir;
    private Vector3 floorVelocity;
    private Rigidbody rb;
    private float acceleration;
    private float gravity;
    private float jumpForce;
    private float friction;
    private float currentAccelFactor = 1f;
    private float currentDecelFactor = 1f;
    private float currentMaxSpeedMultiplier = 1f;
    private float targetAccelFactor = 1f;
    private float targetDecelFactor = 1f;
    private float targetMaxSpeedMultiplier = 1f;
    [SerializeField] private float maxSpeed;
    [SerializeField] private float apexHeight;
    [SerializeField] private float apexTime;
    [SerializeField] private float timeToZero;
    [SerializeField] private float timeToMaxSpeed;
    [SerializeField] private float sprintMaxSpeedMultiplier;
    [SerializeField] private float sprintAccelMultiplier;
    [SerializeField] private float sprintDecelMultiplier;
    [SerializeField] private float crouchMaxSpeedMultiplier;
    [SerializeField] private float crouchAccelMultiplier;
    [SerializeField] private float crouchDecelMultiplier;
    [SerializeField] private float airMaxSpeedMultiplier;
    [SerializeField] private float airAccelMultiplier;
    [SerializeField] private float airDecelMultiplier;
    [SerializeField] private float groundMaxSpeedMultiplier;
    [SerializeField] private float groundAccelMultiplier;
    [SerializeField] private float groundDecelMultiplier;

    [Header("Ground check")]
    [SerializeField] private Transform feetPos;
    [SerializeField] private float feetRadius;
    [SerializeField] private LayerMask groundLayer;

    [Header("Crouching")]
    private float defaultHeight;
    private CapsuleCollider playerCol;

    [Header("Jumping")]
    [Tooltip("Time after not being grounded that we still consider 'grounded'")]
    [SerializeField] private float coyoteTime = 0.1f;
    private bool _isGrounded;
    private float _technicallyStillGroundedTime;
    [SerializeField] private float jumpBufferDuration;
    private float jumpBufferTimer;

    [Header("Slope Handling")]
    private RaycastHit slopeHit;
    private Vector3 slopeNormal;
    private float slopeAngle;

    [Header("Camera")]
    [SerializeField] private float yawSensitivity;
    [SerializeField] private float pitchSensitivity;
    private float yRot;
    private Transform camTransform;

    [Header("Input")]
    private PlayerActions playerActions;

    [Header("States")]
    private PlayerMovementState currentMovementState;
    private PlayerStanceState currentStanceState;
    private float smoothSpeed = 5f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCol = GetComponent<CapsuleCollider>();
        camTransform = Camera.main.transform;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        gravity = 2 * apexHeight / Mathf.Pow(apexTime, 2);
        jumpForce = 2 * apexHeight / apexTime;
        friction = maxSpeed / timeToZero;
        acceleration = maxSpeed / timeToMaxSpeed;
        defaultHeight = playerCol.height;

        playerActions = GetComponent<PlayerActions>();
    }

    private void Update()
    {
        HandleYRot();
        HandleXRot();

        HandleCoyoteTime();
        if(playerActions.JumpDown)
        {
            StartJumpBuffer();
        }

        //Jumping
        if (_isGrounded && jumpBufferTimer > 0f)
        {
            Jump();
            jumpBufferTimer = 0f; //reset jump buffer
        }

        //jump buffer
        if (jumpBufferTimer > 0f)
            jumpBufferTimer -= Time.fixedDeltaTime;

        //stance
        if (playerActions.CrouchHeld)
            SwitchStanceState(PlayerStanceState.Crouching);
        else
            SwitchStanceState(PlayerStanceState.Standing);

    }

    private void FixedUpdate()
    {
        //movement state
        if (IsGrounded())
        {
            if (playerActions.SprintHeld)
                SwitchMovementState(PlayerMovementState.Running);
            else
                SwitchMovementState(PlayerMovementState.Walking);
        }
        else
        {
            SwitchMovementState(PlayerMovementState.InAir);
        }
    }

    private void SwitchMovementState(PlayerMovementState newMovementState)
    {
        currentMovementState = newMovementState;

        switch (currentMovementState)
        {
            case PlayerMovementState.Walking:
                targetAccelFactor = groundAccelMultiplier;
                targetDecelFactor = groundDecelMultiplier;
                targetMaxSpeedMultiplier = groundMaxSpeedMultiplier;
                break;

            case PlayerMovementState.Running:
                targetAccelFactor = sprintAccelMultiplier;
                targetDecelFactor = sprintDecelMultiplier;
                targetMaxSpeedMultiplier = sprintMaxSpeedMultiplier;
                break;

            case PlayerMovementState.InAir:
                targetAccelFactor = airAccelMultiplier;
                targetDecelFactor = airDecelMultiplier;
                targetMaxSpeedMultiplier = airMaxSpeedMultiplier;
                HandleGravity();
                break;
        }

        currentAccelFactor = Mathf.Lerp(currentAccelFactor, targetAccelFactor, Time.fixedDeltaTime * smoothSpeed);
        currentDecelFactor = Mathf.Lerp(currentDecelFactor, targetDecelFactor, Time.fixedDeltaTime * smoothSpeed);
        currentMaxSpeedMultiplier = Mathf.Lerp(currentMaxSpeedMultiplier, targetMaxSpeedMultiplier, Time.fixedDeltaTime * smoothSpeed);

        HandleMovement(currentAccelFactor, currentMaxSpeedMultiplier, currentDecelFactor);
    }

    private void SwitchStanceState(PlayerStanceState newStanceState)
    {
        currentStanceState = newStanceState;

        switch (currentStanceState)
        {
            case PlayerStanceState.Crouching:
                HandlePlayerHeight(defaultHeight / 2f);
                break;

            case PlayerStanceState.Standing:
                HandlePlayerHeight(defaultHeight);
                break;
        }
    }

    private bool IsGrounded()
    {
        feetPos.localPosition = new Vector3(0, -playerCol.height / 2f, 0);
        Collider[] collider;
        collider = Physics.OverlapSphere(feetPos.position, feetRadius, groundLayer);
        if(collider.Length > 0)
        {
            foreach(Collider c in collider)
            {
                if (c.gameObject.GetComponent<Rigidbody>() != null)
                {
                    floorVelocity = c.gameObject.GetComponent<Rigidbody>().linearVelocity;
                    return true;
                }
            }
        }
        else
        {
            floorVelocity = Vector3.zero;
        }

            return collider.Length > 0;
    }

    private void StartJumpBuffer()
    {
        jumpBufferTimer = jumpBufferDuration;

        if (jumpBufferTimer > 0f)
        {
            jumpBufferTimer -= Time.deltaTime;
        }
    }

    private void HandleCoyoteTime()
    {
        if (IsGrounded())
        {
            _isGrounded = true;
        }
        else if (_isGrounded)
        {
            _technicallyStillGroundedTime = Time.time + coyoteTime;
            _isGrounded = false;
        }
    }

    private void HandleYRot()
    {
        //Up-down cam
        yRot -= playerActions.LookInput.y * pitchSensitivity * Time.deltaTime;
        yRot = Mathf.Clamp(yRot, -75f, 75f);

        camTransform.localEulerAngles = new Vector3(yRot, 0f, 0f);
    }

    private void HandleXRot()
    {
        //Sideways rotation
        float newRot = playerActions.LookInput.x * yawSensitivity * Time.fixedDeltaTime;
        Quaternion deltaRotation = Quaternion.Euler(0, newRot, 0f);

        rb.MoveRotation(rb.rotation * deltaRotation);
    }

    private void HandleMovement(float accelFactor, float maxSpeedMultiplier, float decelFactor)
    {
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        Vector3 playerOnlyVel = new Vector3(rb.linearVelocity.x - floorVelocity.x, 0, rb.linearVelocity.z - floorVelocity.z);
        //Debug.Log(floorVelocity);
        //Debug.Log("player: " + transform.TransformDirection(rb.linearVelocity));

        wishDir = new Vector3(playerActions.MoveInput.x, 0, playerActions.MoveInput.y).normalized;

        if (wishDir != Vector3.zero)
        {
            //change input direction to be local
            wishDir = transform.TransformDirection(wishDir);

            //calculate difference in desired velocity and current (self-clamped)
            Vector3 desiredVel = wishDir * (maxSpeed + floorVelocity.magnitude) * maxSpeedMultiplier;
            Vector3 velDelta = desiredVel - horizontalVel;
            Vector3 accelStep = Vector3.ClampMagnitude(velDelta, acceleration * accelFactor * Time.fixedDeltaTime);

            rb.AddForce(accelStep, ForceMode.VelocityChange);
        }
        //else if(currentMovementState == PlayerMovementState.InAir)
        //{
        //    rb.AddForce(wishDir * acceleration * accelFactor * Time.deltaTime, ForceMode.VelocityChange);
        //}
        else
        {
            //Vector3 decelStep = -horizontalVel.normalized * friction * decelFactor * Time.fixedDeltaTime;

            ////prevent overshoot when velocity near 0
            //if (decelStep.sqrMagnitude > horizontalVel.sqrMagnitude)
            //    decelStep = -horizontalVel;

            //rb.AddForce(decelStep, ForceMode.VelocityChange);

            Vector3 decelStep = -playerOnlyVel.normalized * friction * decelFactor * Time.fixedDeltaTime;

            //prevent overshoot when velocity near 0
            if (decelStep.sqrMagnitude > playerOnlyVel.sqrMagnitude)
                decelStep = -playerOnlyVel;

            rb.AddForce(decelStep, ForceMode.VelocityChange);
        }
    }

    private void Jump()
    {
        if(_isGrounded)
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private void HandleGravity()
    {
        rb.AddForce(Vector3.down * gravity, ForceMode.Acceleration);
    }

    private void HandlePlayerHeight(float desiredHeight)
    {
        playerCol.height = Mathf.Lerp(playerCol.height, desiredHeight, Time.fixedDeltaTime * smoothSpeed);
    }
}
