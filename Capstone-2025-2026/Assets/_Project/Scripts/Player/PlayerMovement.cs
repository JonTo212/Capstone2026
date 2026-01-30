using UnityEngine;

public enum PlayerMoveState
{
    Walking,
    InAir,
    Swinging,
    Mantling,
    UsingNPC,
    Grabbing
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
    [SerializeField] private float overshootMaxForce = 10f;
    [SerializeField] private float hardCapMultiplier = 5f;

    [Header("Ground Check")]
    [SerializeField] private Transform feetPos;
    [SerializeField] private float feetRadius;
    [SerializeField] private LayerMask groundLayer;

    [Header("Jump Buffer + Coyote Time + LedgeScan")]
    [SerializeField] private float jumpBufferTime = 0.2f;
    [SerializeField] private float jumpBufferCounter;
    [SerializeField] private float coyoteTime = 0.2f;
    [SerializeField] private float coyoteTimeCounter;
    [SerializeField] private float ledgeScanLength;
    [SerializeField] private float ledgeScanDepth;
    [SerializeField] private float doubleJumpDuration = 0.1f;
    [SerializeField] private float doubleJumpMultiplier = 0.8f;
    //[SerializeField] private Vector2 doubleJumpForce;

    [Header("Camera")]
    [SerializeField] private Camera playerCam;

    [Header("Input")]
    private PlayerSwing _playerSwing;
    private LassoTetherController _lassoTetherController;
    private PlayerWallBounce _playerWallBounce;

    private MovementProperties _currentMultipliers;
    private float _acceleration;
    private float _jumpForce;
    private float _friction;
    private float _maxGravity;
    private bool _useGravity;
    private bool _useFriction;
    private bool _hasJumped;
    private bool _hasDoubleJumped;
    private Vector3 _lastExternalForce;

    public float MovementLockTimer { get; private set; }
    public Vector3 ExternalForce { get; private set; }
    public Vector3 PlayerVelocity { get; private set; }
    public float FrictionMultiplier { get; private set; } = 1f;
    public float Gravity { get; private set; }
    public Vector3 WishDir { get; private set; }
    public Rigidbody Rb { get; private set; }
    public PlayerMoveState CurrentMovementState { get; private set; }
    public PlayerActions PlayerInput { get; private set; }
    public PlayerModelRotationHandler PlayerModelRotationHandler { get; private set; }

    #region Unity Functions
    private void Awake()
    {
        Rb = GetComponent<Rigidbody>();
        PlayerInput = GetComponent<PlayerActions>();
        _lassoTetherController = GetComponent<LassoTetherController>();
        _playerSwing = GetComponent<PlayerSwing>();
        _playerWallBounce = GetComponent<PlayerWallBounce>();
        PlayerModelRotationHandler = GetComponent<PlayerModelRotationHandler>();

        Gravity = 2 * apexHeight / Mathf.Pow(apexTime, 2);
        _jumpForce = 2 * apexHeight / apexTime;
        _maxGravity = Gravity;
        _friction = defaultMaxSpeed / timeToZero;
        _acceleration = defaultMaxSpeed / timeToMaxSpeed;
        CurrentMovementState = PlayerMoveState.InAir;
        _useGravity = true;
        _useFriction = true;

        ExternalForce = Vector3.zero;
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
        HandleMovementLockTimer();
        HandleWalkingSFX();
    }

    private void FixedUpdate()
    {
        HandleMovementState();
        HandleForward();

        if (CurrentMovementState == PlayerMoveState.Grabbing) return;

        Vector3 relVel = Rb.linearVelocity - _lastExternalForce;

        if (_useGravity)
        {
            HandleGravityRelative(ref relVel);
        }

        if (CurrentMovementState == PlayerMoveState.Swinging)
        {
            _playerSwing.HandleSwingMovement(WishDir, ref relVel);
            _playerSwing.ConstrainToRope(ref relVel);
        }
        else
        {
            ApplyAccelerationRelative(ref relVel);
        }

        if (_useFriction)
        {
            ApplyFriction(ref relVel, Vector3.forward);
            ApplyFriction(ref relVel, Vector3.right);
            HandleVelocityOvershootRelative(ref relVel);
        }

        Rb.linearVelocity = relVel + ExternalForce;
        _lastExternalForce = ExternalForce;
    }

    #endregion

    #region Movement State

    public void SwitchMovementState(PlayerMoveState newMovementState)
    {
        if (CurrentMovementState == newMovementState) return;
        CurrentMovementState = newMovementState;

        switch (CurrentMovementState)
        {
            case PlayerMoveState.Walking:
                _currentMultipliers = MovementProperties.Default;
                _hasJumped = false;
                _hasDoubleJumped = false;
                break;

            case PlayerMoveState.InAir:
                _currentMultipliers = _airMultipliers;
                break;

            case PlayerMoveState.Swinging:
                _currentMultipliers = _swingingMultipliers;
                _hasJumped = false;
                _hasDoubleJumped = false;
                break;
        }
    }

    private void HandleMovementState()
    {
        if (CurrentMovementState == PlayerMoveState.Grabbing) return;

        if (_lassoTetherController.CurrentLassoState == LassoState.Swinging)
        {
            SwitchMovementState(PlayerMoveState.Swinging);
        }
        else if (IsGrounded())
        {
            SwitchMovementState(PlayerMoveState.Walking);
        }
        else
        {
            SwitchMovementState(PlayerMoveState.InAir);
        }
    }

    public void SetGrabbing(bool isGrabbing)
    {
        if (isGrabbing) SwitchMovementState(PlayerMoveState.Grabbing);
        else SwitchMovementState(PlayerMoveState.InAir);
    }

    private void HandleMovementLockTimer()
    {
        if (MovementLockTimer <= 0) return;
        MovementLockTimer -= Time.deltaTime;
    }
    #endregion

    #region Misc

    public void InheritPlatformMomentum(Vector3 externalForce)
    {
        ExternalForce = externalForce;
        _lastExternalForce = externalForce;
    }

    public void SetExternalForce(Vector3 externalForce)
    {
        ExternalForce = externalForce;
    }

    private void HandleWalkingSFX()
    {
        if (WishDir != Vector3.zero && IsGrounded())
        {
            AudioManager.Instance.SFXSource7.UnPause();
        }
        else
        {
            AudioManager.Instance.SFXSource7.Pause(); //PlaySFX(aManage.Walk, 5, 1);
        }
    }
    #endregion

    #region Detection
    public Transform IsGrounded()
    {
        Ray downwardRay = new Ray(transform.position, Vector3.down);
        Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1.1f, groundLayer);

        return hit.transform;
    }
    #endregion

    #region Forward / Camera
    public void HandleForward()
    {
        if (MovementLockTimer > 0) return;

        Vector3 camForward = playerCam.transform.forward;
        Vector3 camRight = playerCam.transform.right;

        camRight.y = 0;
        camForward.y = 0;

        Vector3 forwardRelative = camForward * PlayerInput.MoveInput.y;
        Vector3 rightRelative = camRight * PlayerInput.MoveInput.x;

        Vector3 desiredDir = Vector3.ClampMagnitude(forwardRelative + rightRelative, 1f);
        WishDir = (forwardRelative + rightRelative).normalized;
        //WishDir = LedgeCheckWithoutAForLoop(desiredDir);
    }
    #endregion

    #region Jumping

    public bool useDoubleJump; //ALSO THIS SHIT

    private void HandleJumpBuffer()
    {
        //Jump Buffer
        if (PlayerInput.JumpDown)
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
        bool grounded = IsGrounded() && Rb.linearVelocity.y <= 0f; //THIS MAKES DOUBLE JUMP WEIRD

        if (IsGrounded())
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
    }

    private void HandleJump()
    {
        if (jumpBufferCounter > 0)
        {
            //wall bounce
            if (_playerWallBounce != null && _playerWallBounce.TryWallBounce())
            {
                jumpBufferCounter = 0;
                AudioManager.Instance.PlaySFX(AudioManager.Instance.Jump, 6, 1f);
                _hasJumped = true;
                return;
            }

            //regular jump
            if (!_hasJumped && coyoteTimeCounter > 0f)
            {
                coyoteTimeCounter = 0;
                jumpBufferCounter = 0;
                AudioManager.Instance.PlaySFX(AudioManager.Instance.Jump, 6, 1f);

                Jump(1f);
            }

            else if(_hasJumped && !_hasDoubleJumped && useDoubleJump)
            {
                //Jump(doubleJumpMultiplier);
                HandleDoubleJump();
                _hasDoubleJumped = true;
            }
        }
    }

    public void Jump(float multiplier)
    {
        Vector3 jumpForce = Vector3.up * _jumpForce * multiplier;

        Rb.linearVelocity = new Vector3(Rb.linearVelocity.x, 0, Rb.linearVelocity.z);
        Rb.AddForce(jumpForce, ForceMode.Impulse);
        _hasJumped = true;
    }

    private void HandleDoubleJump()
    {
        /*//v1: original double jump that adds to current velocity
        Vector3 newVel = WishDir * doubleJumpForce.x + Vector3.up * doubleJumpForce.y;
        Vector3 currentVel = Rb.linearVelocity;

        //if falling, cancel downward momentum
        float newY = currentVel.y;
        if (newY < 0)
            newY = 0;

        //cancel momentum opposite to wishDir
        Vector3 newHorizontal = new Vector3(currentVel.x, 0, currentVel.z);
        float currentSpeed = newHorizontal.magnitude;

        if (WishDir.sqrMagnitude > 0)
        {
            float dot = Vector3.Dot(newHorizontal.normalized, WishDir);
            if (dot < 0)
                newHorizontal = Vector3.zero;
        }

        Rb.linearVelocity = new Vector3(newHorizontal.x, newY, newHorizontal.z);
        Rb.AddForce(newVel, ForceMode.Impulse);
        //PlayerModelRotationHandler.SetNewRotationDir(WishDir, doubleJumpDuration);

        _hasDoubleJumped = true;
        MovementLockTimer = doubleJumpDuration;*/

        //v2: redirect current horizontal velocity to wishDir, set vertical to jump force
        Vector3 horizontalVel = new Vector3(Rb.linearVelocity.x, 0, Rb.linearVelocity.z);
        float speed = horizontalVel.magnitude;

        Vector3 redirectedVel = horizontalVel;
        if (WishDir.sqrMagnitude > 0)
            redirectedVel = WishDir.normalized * speed;

        Vector3 defaultJumpForce = Vector3.up * _jumpForce * doubleJumpMultiplier;
        //Vector3 addedJumpForce = WishDir.normalized * doubleJumpForce.x + Vector3.up * doubleJumpForce.y;

        Rb.linearVelocity = redirectedVel + defaultJumpForce; // + addedJumpForce;
        _hasDoubleJumped = true;
        MovementLockTimer = doubleJumpDuration;
    }
    #endregion

    #region Gravity
    private void HandleGravityRelative(ref Vector3 relVel)
    {
        if (CurrentMovementState == PlayerMoveState.Walking) return;

        //apply gravity to the reference variable instead of addForce
        relVel.y -= Gravity * Time.fixedDeltaTime;
        if (relVel.y < -_maxGravity)
        {
            relVel.y = -_maxGravity;
        }
    }

    public void ApplySlowFall(float multiplier)
    {
        if (Rb.linearVelocity.y < 0)
        {
            Gravity = _maxGravity * multiplier;

            if (Rb.linearVelocity.y <= -Gravity)
            {
                Rb.linearVelocity = new Vector3(Rb.linearVelocity.x, -Gravity, Rb.linearVelocity.z);
            }
        }
        else
        {
            Gravity = _maxGravity;
        }
    }

    public void EnableGravity(bool enable)
    {
        _useGravity = enable;
    }

    public void ResetGravity()
    {
        Gravity = _maxGravity;
    }
    #endregion

    #region Velocity Overshoot
    private void HandleVelocityOvershootRelative(ref Vector3 relVel)
    {
        //I THINK THIS + FRICTION ARE STILL CAUSING STICKING ISSUES
        float hardMax = defaultMaxSpeed * hardCapMultiplier;
        float currentMax = _currentMultipliers.maxSpeedMultiplier * defaultMaxSpeed;

        Vector3 horizontalRel = new Vector3(relVel.x, 0, relVel.z);
        float speed = horizontalRel.magnitude;

        if (speed > currentMax)
        {
            float t = Mathf.InverseLerp(currentMax, hardMax, speed); //gives 0-1 based on how far between currentMax and hardMax the speed is
            float easedT = Mathf.SmoothStep(0f, 1f, t);
            float decayStrength = overshootMaxForce * easedT;

            if (!IsGrounded()) decayStrength /= 2f;

            float newSpeed = Mathf.MoveTowards(speed, currentMax, decayStrength * Time.fixedDeltaTime);
            relVel.x = (horizontalRel.x / speed) * newSpeed;
            relVel.z = (horizontalRel.z / speed) * newSpeed;
        }
    }
    #endregion

    #region Friction
    public void ApplyFriction(ref Vector3 playerVel, Vector3 frictionAxis)
    {
        if (MovementLockTimer > 0) return;
        if (frictionAxis.sqrMagnitude < 0.0001f) return;

        frictionAxis.Normalize();
        Vector3 velocityOnAxis = Vector3.Project(playerVel, frictionAxis);

        if (velocityOnAxis.sqrMagnitude < 0.0001f) return;

        float speed = velocityOnAxis.magnitude;
        Vector3 frictionDir = -velocityOnAxis.normalized;
        float frictionAccel;

        //stopping friction -> bring you to a stop
        if (WishDir == Vector3.zero || FrictionMultiplier < 1f)
        {
            float stopAccel = speed / Time.fixedDeltaTime;
            float rawFriction = _friction * _currentMultipliers.decelMultiplier;
            frictionAccel = Mathf.Min(rawFriction, stopAccel);
        }
        //movement friction -> keep you at maxSpeed
        else
        {
            float targetSpeed = defaultMaxSpeed * _currentMultipliers.maxSpeedMultiplier;
            frictionAccel = _acceleration * _currentMultipliers.accelMultiplier * (speed / targetSpeed);
        }

        float deltaV = frictionAccel * FrictionMultiplier * Time.fixedDeltaTime;
        float newSpeed = Mathf.Max(0f, speed - deltaV);
        playerVel = (playerVel - velocityOnAxis) + (velocityOnAxis.normalized * newSpeed);
    }
    public void EnableFriction(bool enable)
    {
        _useFriction = enable;
    }


    #endregion

    #region Acceleration
    private void ApplyAccelerationRelative(ref Vector3 relVel)
    {
        if (WishDir == Vector3.zero) return;

        //apply accel to reference (same as Forcemode.Accel)
        Vector3 accel = WishDir * _acceleration * _currentMultipliers.accelMultiplier;
        relVel += accel * Time.fixedDeltaTime;
    }
    #endregion

    #region Ledge Handling
    private Vector3 LedgeCheck()
    {
        //If there is a place the player can fall, the check will return where that is

        Vector3 fallOffArea = new Vector3(0, 0, 0);
        for (int i = -1; i < 2; i++)
        {
            for (int j = -1; j < 2; j++)
            {
                if (Physics.Raycast(transform.position, new Vector3(i, 0, j), out RaycastHit hit, ledgeScanLength))
                {
                    if (Physics.Raycast(hit.point, Vector3.down, out RaycastHit hitDown, ledgeScanDepth))
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
    #endregion
}
