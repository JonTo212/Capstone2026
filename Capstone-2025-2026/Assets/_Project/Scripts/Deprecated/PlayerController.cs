using UnityEngine;

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

[System.Serializable]
public struct MovementMultiplier
{
    public float accelMultiplier;
    public float decelMultiplier;
    public float maxSpeedMultiplier;

    public MovementMultiplier(float accel, float decel, float max)
    {
        accelMultiplier = accel;
        decelMultiplier = decel;
        maxSpeedMultiplier = max;
    }
}
public class PlayerController : MonoBehaviour
{
    [Header("Movement variables")]
    private Vector3 wishDir;
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
    private Vector3 accelVector;
    [SerializeField] private float maxSpeed;
    [SerializeField] private float apexHeight;
    [SerializeField] private float apexTime;
    [SerializeField] private float timeToZero;
    [SerializeField] private float timeToMaxSpeed;
    [SerializeField] private MovementMultiplier sprintMultipliers;
    [SerializeField] private MovementMultiplier crouchMultipliers;
    [SerializeField] private MovementMultiplier airMultipliers;
    [SerializeField] private MovementMultiplier groundMultipliers;

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
    private LassoTetherController lassoTetherController;
    private float smoothSpeed = 5f;

    public float Gravity => gravity;
    public Vector3 AccelVector => accelVector;
    public Vector3 WishDir => wishDir;
    public Rigidbody Rb => rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCol = GetComponent<CapsuleCollider>();
        lassoTetherController = GetComponent<LassoTetherController>();
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
        /*if (playerActions.CrouchHeld)
            SwitchStanceState(PlayerStanceState.Crouching);
        else*/
            SwitchStanceState(PlayerStanceState.Standing);

    }

    private void FixedUpdate()
    {
        //movement state
        if (IsGrounded())
        {
            /*if (playerActions.SprintHeld)
                SwitchMovementState(PlayerMovementState.Running);
            else*/
                SwitchMovementState(PlayerMovementState.Walking);
        }
        else
        {
            SwitchMovementState(PlayerMovementState.InAir);

            if(lassoTetherController != null && lassoTetherController.CurrentLassoState == LassoState.Swinging) //temp override
                targetAccelFactor = airMultipliers.accelMultiplier;
                targetDecelFactor = airMultipliers.decelMultiplier;
                targetMaxSpeedMultiplier = sprintMultipliers.maxSpeedMultiplier;
        }

        CalculateMovementMultipliers();
        HandleMovement(currentAccelFactor, currentMaxSpeedMultiplier, currentDecelFactor);
    }

    private void SwitchMovementState(PlayerMovementState newMovementState)
    {
        currentMovementState = newMovementState;

        switch (currentMovementState)
        {
            case PlayerMovementState.Walking:
                targetAccelFactor = groundMultipliers.accelMultiplier;
                targetDecelFactor = groundMultipliers.decelMultiplier;
                targetMaxSpeedMultiplier = groundMultipliers.maxSpeedMultiplier;
                break;

            case PlayerMovementState.Running:
                targetAccelFactor = sprintMultipliers.accelMultiplier;
                targetDecelFactor = sprintMultipliers.decelMultiplier;
                targetMaxSpeedMultiplier = sprintMultipliers.maxSpeedMultiplier;
                break;

            case PlayerMovementState.InAir:
                targetAccelFactor = airMultipliers.accelMultiplier;
                targetDecelFactor = airMultipliers.decelMultiplier;
                targetMaxSpeedMultiplier = airMultipliers.maxSpeedMultiplier;
                HandleGravity();
                break;
        }

        
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

    private void CalculateMovementMultipliers()
    {
        currentAccelFactor = Mathf.Lerp(currentAccelFactor, targetAccelFactor, Time.fixedDeltaTime * smoothSpeed);
        currentDecelFactor = targetDecelFactor;
        currentMaxSpeedMultiplier = Mathf.Lerp(currentMaxSpeedMultiplier, targetMaxSpeedMultiplier, Time.fixedDeltaTime * smoothSpeed);
    }

    private bool IsGrounded()
    {
        feetPos.localPosition = new Vector3(0, -playerCol.height / 2f, 0);
        return Physics.CheckSphere(feetPos.position, feetRadius, groundLayer);
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
        wishDir = new Vector3(playerActions.MoveInput.x, 0, playerActions.MoveInput.y).normalized;

        if (wishDir != Vector3.zero)
        {
            //change input direction to be local
            wishDir = transform.TransformDirection(wishDir);

            //calculate difference in desired velocity and current (self-clamped)
            Vector3 desiredVel = wishDir * maxSpeed * maxSpeedMultiplier;
            Vector3 velDelta = desiredVel - horizontalVel;
            accelVector = velDelta.normalized * acceleration * accelFactor;

            //accel = m/s^2
            if (accelVector.sqrMagnitude > velDelta.sqrMagnitude / (Time.fixedDeltaTime * Time.fixedDeltaTime))
                accelVector = velDelta / Time.fixedDeltaTime;

            rb.AddForce(accelVector, ForceMode.Acceleration);
        }
        else
        {
            Vector3 decelStep = -horizontalVel.normalized * friction * decelFactor;

            //prevent overshoot when near zero
            if (decelStep.sqrMagnitude > (horizontalVel.sqrMagnitude / (Time.fixedDeltaTime * Time.fixedDeltaTime)))
                decelStep = -horizontalVel / Time.fixedDeltaTime;

            rb.AddForce(decelStep, ForceMode.Acceleration);
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
