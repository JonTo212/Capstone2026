using UnityEngine;

public enum PlayerMoveState
{
    Walking,
    InAir,
    Swinging,
    Mantling,
    UsingNPC
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
    [SerializeField] private MovementProperties _swingingMultipliers;
    [SerializeField] private float overshootCorrectionMultiplier = 10f;
    [SerializeField] private float hardCapMultiplier = 5f;

    [Header("Ground Check")]
    [SerializeField] private Transform feetPos;
    [SerializeField] private float feetRadius;
    [SerializeField] private LayerMask groundLayer;
    private bool hasJumped = false;

    [Header("Jump Buffer + Coyote Time + LedgeScan")]
    [SerializeField] private float jumpBufferTime = 0.2f;
    [SerializeField] private float jumpBufferCounter;
    [SerializeField] private float coyoteTime = 0.2f;
    [SerializeField] private float coyoteTimeCounter;
    [SerializeField] private float ledgeScanLength;
    [SerializeField] private float ledgeScanDepth;

    [Header("Camera")]
    //[SerializeField] private float yawSensitivity;
    //[SerializeField] private float pitchSensitivity;
    [SerializeField] private Camera playerCam;
    //[SerializeField] private float desiredSwingFOV = 90f;
    //[SerializeField] private float desiredHoldFOV = 75f;
    //[SerializeField] private float FOVChangeSpeed = 5f;

    [Header("Input")]
    private PlayerActions _playerActions;
    private PlayerSwing _playerSwing;
    private LassoTetherController _lassoTetherController;

    private MovementProperties _currentMultipliers;
    private PlayerMoveState _currentMovementState;
    private Vector3 _wishDir;
    private Rigidbody _rb;
    private CapsuleCollider _playerCol;
    private float _acceleration;
    private float _gravity;
    private float _jumpForce;
    private float _friction;
    private float _defaultFOV;
    private float _maxGravity;
    private bool _useGravity;

    public Vector3 ExternalForce { get; set; }
    public float Gravity => _gravity;
    public Vector3 WishDir => _wishDir;
    public Rigidbody Rb => _rb;
    public float Acceleration => _acceleration;
    public float DefaultMaxSpeed => defaultMaxSpeed;
    public MovementProperties CurrentMultipliers => _currentMultipliers;
    public PlayerMoveState CurrentMovementState => _currentMovementState;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _playerCol = GetComponent<CapsuleCollider>();
        _playerActions = GetComponent<PlayerActions>();
        _lassoTetherController = GetComponent<LassoTetherController>();
        _playerSwing = GetComponent<PlayerSwing>();

        _gravity = 2 * apexHeight / Mathf.Pow(apexTime, 2);
        _jumpForce = 2 * apexHeight / apexTime;
        _maxGravity = _gravity;
        _friction = defaultMaxSpeed / timeToZero;
        _acceleration = defaultMaxSpeed / timeToMaxSpeed;
        _defaultFOV = playerCam.fieldOfView;
        _currentMovementState = PlayerMoveState.InAir;
        _useGravity = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Start()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.Walk, 7, 1);
        AudioManager.Instance.SFXSource7.loop = true;
    }

    private void Update()
    {
        HandleJumpBuffer();
        HandleCoyoteTime();
        HandleJump();
        //HandleFOV();
        HandleWalkingSFX();
    }

    private void FixedUpdate()
    {
        HandleMovementState();
        HandleForward();

        if (_useGravity)
        {
            HandleGravity();
        }

        if (_currentMovementState == PlayerMoveState.Swinging)
        {
            _playerSwing.HandleSwingMovement(_wishDir);
            _playerSwing.ConstrainToRope();
        }
        else
        {
            ApplyAcceleration();
        }

        ApplyFriction(Vector3.forward);
        ApplyFriction(Vector3.right);
        HandleVelocityOvershoot();
    }

    public void SwitchMovementState(PlayerMoveState newMovementState)
    {
        if(_currentMovementState == newMovementState) return;
        _currentMovementState = newMovementState;

        switch (_currentMovementState)
        {
            case PlayerMoveState.Walking:
                _currentMultipliers = MovementProperties.Default;
                break;

            case PlayerMoveState.InAir:
                _currentMultipliers = _airMultipliers;
                break;

            case PlayerMoveState.Swinging:
                _currentMultipliers = _swingingMultipliers;
                break;
        }
    }

    public bool IsGrounded()
    {
        //feetPos.localPosition = new Vector3(0, -_playerCol.height / 2f, 0);
        //return Physics.CheckSphere(feetPos.position, feetRadius, groundLayer);

        return (Physics.Raycast(transform.position, Vector3.down, 1.05f, groundLayer));
    }

    public void ApplySlowFall(float multiplier)
    {
        if (_rb.linearVelocity.y < 0)
        {
            _gravity = _maxGravity * multiplier;

            if (_rb.linearVelocity.y <= -_gravity)
            {
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, -_gravity, _rb.linearVelocity.z);
            }
        }
        else
        {
            _gravity = _maxGravity;
        }
    }

    public void EnableGravity(bool enable)
    {
        _useGravity = enable;
    }

    public void ResetGravity()
    {
        _gravity = _maxGravity;
    }

    private void HandleMovementState()
    {
        if (_lassoTetherController.CurrentLassoState == LassoState.Swinging)
        {
            SwitchMovementState(PlayerMoveState.Swinging);
        }
        else if (IsGrounded())
        {
            SwitchMovementState(PlayerMoveState.Walking);
            hasJumped = false;
        }
        else
        {
            SwitchMovementState(PlayerMoveState.InAir);
        }
    }

    private void HandleWalkingSFX()
    {
        if (_wishDir != Vector3.zero && IsGrounded())
        {
            AudioManager.Instance.SFXSource7.UnPause();
        }
        else
        {
            AudioManager.Instance.SFXSource7.Pause(); //PlaySFX(aManage.Walk, 5, 1);
        }
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


    /*private void HandleFOV()
    {
        float desiredFOV = _defaultFOV;
        float changeSpeed = FOVChangeSpeed;

        if (_lassoTetherController.CurrentLassoState == LassoState.Swinging)
        {
            desiredFOV = desiredSwingFOV;
            changeSpeed = FOVChangeSpeed;
        }
        else if (_lassoTetherController.CurrentLassoState == LassoState.Snared || _lassoTetherController.CurrentLassoState == LassoState.SnaredTether)
        {
            desiredFOV = desiredHoldFOV;
            changeSpeed = FOVChangeSpeed * 2f;
        }

        playerCam.fieldOfView = Mathf.SmoothStep(playerCam.fieldOfView, desiredFOV, Time.deltaTime * changeSpeed);
    }*/

    public void HandleForward()
    {
        Vector3 camForward = playerCam.transform.forward;
        Vector3 camRight = playerCam.transform.right;

        camRight.y = 0;
        camForward.y = 0;

        Vector3 forwardRelative = camForward * _playerActions.MoveInput.y;
        Vector3 rightRelative = camRight * _playerActions.MoveInput.x;
        
        _wishDir = LedgeCheckWithoutAForLoop((forwardRelative + rightRelative).normalized);
        

    }

    private void HandleJump()
    {
        if ((jumpBufferCounter > 0) && (coyoteTimeCounter > 0) && !hasJumped)
        {
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
            _rb.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);

            coyoteTimeCounter = 0f;
            jumpBufferCounter = 0;
            AudioManager.Instance.PlaySFX(AudioManager.Instance.Jump, 6, 1f);
            hasJumped = true;
        }
    }

    private void HandleGravity()
    {
        if (_currentMovementState == PlayerMoveState.Walking) return;
        if (_rb.linearVelocity.y <= 0 && _rb.linearVelocity.y <= - _maxGravity)
        {
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, -_maxGravity, _rb.linearVelocity.z);
            return;
        }

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

    public void ApplyFriction(Vector3 frictionAxis)
    {
        frictionAxis.Normalize();
        Vector3 velocityOnAxis = Vector3.Project(_rb.linearVelocity, frictionAxis);
        float speed = velocityOnAxis.magnitude;

        if (speed <= 0f)
        {
            return;
        }

        Vector3 frictionDir = -velocityOnAxis.normalized;

        if (_wishDir == Vector3.zero)
        {
            float stopAccel = speed / Time.fixedDeltaTime;
            float frictionAccel = _friction * _currentMultipliers.decelMultiplier;
            float finalAccel = Mathf.Min(frictionAccel, stopAccel);

            _rb.AddForce(frictionDir * finalAccel, ForceMode.Acceleration);
        }
        else
        {
            float targetSpeed = defaultMaxSpeed * _currentMultipliers.maxSpeedMultiplier;
            float frictionAccel = _acceleration * _currentMultipliers.accelMultiplier * (speed / targetSpeed);

            _rb.AddForce(frictionDir * frictionAccel, ForceMode.Acceleration);
        }
    }

    private void ApplyAcceleration()
    {
        if (_wishDir == Vector3.zero) return;

        Vector3 accelForce = _wishDir * _acceleration * _currentMultipliers.accelMultiplier;
        if (ExternalForce != Vector3.zero) accelForce = ExternalForce;

        _rb.AddForce(accelForce, ForceMode.Acceleration);
    }

    private Vector3 LedgeCheck()
    {
        //If there is a place the player can fall, the check will return where that is

        Vector3 fallOffArea = new Vector3(0, 0, 0);
        for(int i = -1; i < 2; i++)
        {
            for(int j = -1; j < 2; j++)
            {
                if(Physics.Raycast(transform.position, new Vector3(i,0,j), out RaycastHit hit, ledgeScanLength))
                {
                    if(Physics.Raycast(hit.point, Vector3.down, out RaycastHit hitDown, ledgeScanDepth))
                    {
                        return fallOffArea = new Vector3(i, 0, j);
                    }
                }
            }
        }


        return fallOffArea;
    }

    private Vector3 LedgeCheckWithoutAForLoop(Vector3 intendedDirection)
    { 
        //If there is a place the player can fall, the check will return where that is
        
        if (!Physics.Raycast(feetPos.position + Vector3.up * 1.9f + (intendedDirection * ledgeScanLength), Vector3.down, ledgeScanDepth) && IsGrounded()
            && (_lassoTetherController.CurrentLassoState == LassoState.Snared || _lassoTetherController.CurrentLassoState == LassoState.Tethering || 
            _lassoTetherController.CurrentLassoState == LassoState.SnaredTether))
        {
            intendedDirection = Vector3.zero;
        }

        Debug.DrawLine(feetPos.position + (intendedDirection * ledgeScanLength), feetPos.position + (intendedDirection * ledgeScanLength) + (Vector3.down * ledgeScanDepth), Color.red);
        return intendedDirection;
    }

}
