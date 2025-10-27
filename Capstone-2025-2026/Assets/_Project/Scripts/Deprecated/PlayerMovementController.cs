using UnityEngine;
using static UnityEngine.InputSystem.DefaultInputActions;

public enum PlayerState
{
    Walking,
    InAir
}

public class PlayerMovementController : Object
{
    [Header("Movement variables")]
    private Vector3 _wishDir;
    [SerializeField] private float _acceleration;
    private float _jumpForce;

    [SerializeField] private float _maxSpeed;

    [Header("Ground check")]
    [SerializeField] private Transform feetPos;
    [SerializeField] private float feetRadius;
    [SerializeField] private LayerMask groundLayer;

    [Header("Jumping")]
    [Tooltip("Time after not being grounded that we still consider 'grounded'")]
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferDuration;
    private bool _isGrounded;
    private float _technicallyStillGroundedTime;
    private float _jumpBufferTimer;

    [Header("Slope Handling")]
    private RaycastHit _slopeHit;
    private Vector3 _slopeNormal;
    private float _slopeAngle;

    [Header("Camera")]
    [SerializeField] private float yawSensitivity;
    [SerializeField] private float pitchSensitivity;
    [SerializeField] private Transform camTransform;
    private float _yRot;


    [Header("Input")]
    private PlayerActions _playerActions;

    [Header("States")]
    private PlayerState _currentPlayerState;

    private void Awake()
    {
        Init();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _playerActions = GetComponent<PlayerActions>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        HandleCoyoteTime();
        if (_playerActions.JumpDown)
        {
            StartJumpBuffer();
        }
    }

    private void LateUpdate()
    {
       
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        HandleMovement();

        HandleYRot();
        HandleXRot();
    }

    private void HandleYRot()
    {
        //Up-down cam
        _yRot -= _playerActions.LookInput.y * pitchSensitivity * Time.deltaTime;
        _yRot = Mathf.Clamp(_yRot, -75f, 75f);

        camTransform.localEulerAngles = new Vector3(_yRot, 0f, 0f);
    }

    private void HandleXRot()
    {
        //Sideways rotation
        float newRot = _playerActions.LookInput.x * yawSensitivity * Time.fixedDeltaTime;
        Quaternion deltaRotation = Quaternion.Euler(0, newRot, 0f);

        Rb.MoveRotation(Rb.rotation * deltaRotation);
    }

    private void HandleMovement()
    {
        Vector3 horizontalVel = new Vector3(Rb.linearVelocity.x, 0, Rb.linearVelocity.z);
        _wishDir = new Vector3(_playerActions.MoveInput.x, 0, _playerActions.MoveInput.y).normalized;

        if (_wishDir != Vector3.zero)
        {
            //change input direction to be local
            _wishDir = transform.TransformDirection(_wishDir);

            ApplyForce(_wishDir, _acceleration, ForceMode.Acceleration);
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

    private bool IsGrounded()
    {
        return Physics.CheckSphere(feetPos.position, feetRadius, groundLayer);
    }

    private void StartJumpBuffer()
    {
        _jumpBufferTimer = jumpBufferDuration;

        if (_jumpBufferTimer > 0f)
        {
            _jumpBufferTimer -= Time.deltaTime;
        }
    }

    private void Jump()
    {
        if (_isGrounded)
            Rb.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
    }
}
