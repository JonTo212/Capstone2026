using UnityEngine;

public enum PlayerMoveState
{
    Walking,
    InAir,
    Swinging,
    OnMovingPlatform 
}

[System.Serializable]
public struct MovementProperties
{
    public float accelMultiplier;
    public float decelMultiplier;
    public float maxSpeedMultiplier;

    public MovementProperties(float accel, float decel, float max)
    {
        accelMultiplier = accel;
        decelMultiplier = decel;
        maxSpeedMultiplier = max;
    }

    public static MovementProperties Default => new MovementProperties(1f, 1f, 1f);
}

public class PlayerMovement : MonoBehaviour
{
    [Header("Base Movement Settings")]
    [SerializeField] private float defaultMaxSpeed;
    [SerializeField] private float apexHeight;
    [SerializeField] private float apexTime;
    [SerializeField] private float timeToZero;
    [SerializeField] private float timeToMaxSpeed;

    [Header("Multipliers")]
    [SerializeField] private MovementProperties _airMultipliers;
    [SerializeField] private float overshootCorrectionMultiplier = 10f;
    [SerializeField] private float hardCapMultiplier = 5f;

    [Header("Ground Check")]
    [SerializeField] private Transform feetPos;
    [SerializeField] private float feetRadius;
    [SerializeField] private LayerMask groundLayer;

    [Header("Jump Buffer + Coyote Time")]
    [SerializeField] private float jumpBufferTime = 0.2f;
    [SerializeField] private float jumpBufferCounter;
    [SerializeField] private float coyoteTime = 0.2f;
    [SerializeField] private float coyoteTimeCounter;

    [Header("Camera")]
    [SerializeField] private float yawSensitivity;
    [SerializeField] private float pitchSensitivity;
    [SerializeField] private Camera playerCam;

    [Header("Input")]
    private PlayerActions _playerActions;

    private MovementProperties _currentMultipliers;
    private PlayerMoveState _currentMovementState;
    private Vector3 _wishDir;
    private Vector3 _slopeNormal;
    private RaycastHit _slopeHit;
    private Rigidbody _rb;
    private CapsuleCollider _playerCol;
    private float _yRot;
    private float _acceleration;
    private float _gravity;
    private float _jumpForce;
    private float _friction;
    private float _slopeAngle;

    public float ExternalForce { get; set; }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _playerCol = GetComponent<CapsuleCollider>();
        _playerActions = GetComponent<PlayerActions>();

        _gravity = 2 * apexHeight / Mathf.Pow(apexTime, 2);
        _jumpForce = 2 * apexHeight / apexTime;
        _friction = defaultMaxSpeed / timeToZero;
        _acceleration = defaultMaxSpeed / timeToMaxSpeed;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleRot();
        HandleJumpBuffer();
        HandleCoyoteTime();
        HandleJump();
    }

    private void FixedUpdate()
    {
        if (IsGrounded())
        {
            SwitchMovementState(PlayerMoveState.Walking);
        }
        else
        {
            SwitchMovementState(PlayerMoveState.InAir);
        }

        HandleMovement();
        HandleVelocityOvershoot();
    }

    private void SwitchMovementState(PlayerMoveState newMovementState)
    {
        _currentMovementState = newMovementState;

        switch (_currentMovementState)
        {
            case PlayerMoveState.Walking:
                _currentMultipliers = MovementProperties.Default;
                break;

            case PlayerMoveState.InAir:
                _currentMultipliers = _airMultipliers;
                HandleGravity();
                break;
        }
    }

    private bool IsGrounded()
    {
        feetPos.localPosition = new Vector3(0, -_playerCol.height / 2f, 0);
        return Physics.CheckSphere(feetPos.position, feetRadius, groundLayer);
    }

    private void HandleJumpBuffer()
    {
        //Jump Buffer
        if (_playerActions.JumpDown)
        {
            //reset timer
            jumpBufferCounter = jumpBufferTime;

        }
        else if (jumpBufferCounter > 0)
        {
            //count down timer
            jumpBufferCounter -= Time.deltaTime;
        }
    }

    private void HandleCoyoteTime()
    {
        if (IsGrounded())
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            //countdown timer
            coyoteTimeCounter -= Time.deltaTime;
        }
    }

    private void HandleRot()
    {
        //Up-down cam
        _yRot -= _playerActions.LookInput.y * pitchSensitivity * Time.deltaTime;
        _yRot = Mathf.Clamp(_yRot, -75f, 75f);

        playerCam.transform.localEulerAngles = new Vector3(_yRot, 0f, 0f);

        //Sideways rotation
        float newRot = _playerActions.LookInput.x * yawSensitivity * Time.fixedDeltaTime;
        Quaternion deltaRotation = Quaternion.Euler(0, newRot, 0f);

        _rb.MoveRotation(_rb.rotation * deltaRotation);

    }

    private void HandleMovement()
    {
        Vector3 horizontalVel = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);

        _wishDir = new Vector3(_playerActions.MoveInput.x, 0, _playerActions.MoveInput.y).normalized;

        if (_wishDir != Vector3.zero)
        {
            //change input direction to be local
            _wishDir = transform.TransformDirection(_wishDir);

            //calculate difference in desired velocity and current (self-clamped)
            Vector3 desiredVel = _wishDir * defaultMaxSpeed * _currentMultipliers.maxSpeedMultiplier;
            Vector3 velDelta = desiredVel - horizontalVel;
            Vector3 accelStep = Vector3.ClampMagnitude(velDelta, _acceleration * _currentMultipliers.accelMultiplier * Time.fixedDeltaTime);

            _rb.AddForce(accelStep, ForceMode.VelocityChange);
        }
        else
        {
            Vector3 decelStep = -horizontalVel.normalized * _friction * _currentMultipliers.decelMultiplier;

            //prevent overshoot when near zero
            if (decelStep.sqrMagnitude > (horizontalVel.sqrMagnitude / (Time.fixedDeltaTime * Time.fixedDeltaTime)))
                decelStep = -horizontalVel / Time.fixedDeltaTime;

            _rb.AddForce(decelStep, ForceMode.Acceleration);
        }
    }

    private void HandleJump()
    {
        if ((jumpBufferCounter > 0) && (coyoteTimeCounter > 0))
        {
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
            _rb.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);

            coyoteTimeCounter = 0f;
            jumpBufferCounter = 0;
        }
    }

    private void HandleGravity()
    {
        _rb.AddForce(Vector3.down * _gravity, ForceMode.Acceleration);
    }

    private void HandleVelocityOvershoot()
    {
        float currentMax = _currentMultipliers.maxSpeedMultiplier * defaultMaxSpeed;
        float hardCap = currentMax * hardCapMultiplier;
        Vector3 vel = _rb.linearVelocity;
        Vector3 absVel = new Vector3(Mathf.Abs(vel.x), Mathf.Abs(vel.y), Mathf.Abs(vel.z));
        Vector3 correction = Vector3.zero;

        if (absVel.x > currentMax)
        {
            float overshootX = absVel.x - currentMax;
            float t = Mathf.Clamp01(overshootX / (hardCap - currentMax));
            float accel = overshootX * t;
            correction.x = -Mathf.Sign(vel.x) * accel;
        }

        if (absVel.z > currentMax)
        {
            float overshootZ = absVel.z - currentMax;
            float t = Mathf.Clamp01(overshootZ / (hardCap - currentMax));
            float accel = overshootZ * t;
            correction.z = -Mathf.Sign(vel.z) * accel;
        }

        if (correction != Vector3.zero) _rb.AddForce(correction * overshootCorrectionMultiplier, ForceMode.Acceleration);
    }

    private void HandleMovingPlatforms()
    {

    }
}
