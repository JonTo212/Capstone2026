using DG.Tweening;
using FMODUnity;
using System;
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
    [SerializeField] private float overshootMaxForce = 50f;
    [SerializeField] private float hardCapMultiplier = 5f;
    [SerializeField] private float steeringMultiplier = 1.5f;

    [Header("Ground Check")]
    [SerializeField] private Transform feetPos;
    [SerializeField] private float feetRadius;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float maxSlopeAngle = 45f;

    [Header("Jump Buffer + Coyote Time + Double Jump")]
    [SerializeField] private float jumpBufferTime = 0.2f;
    [SerializeField] private float jumpBufferCounter;
    [SerializeField] private float coyoteTime = 0.2f;
    [SerializeField] private float coyoteTimeCounter;
    [SerializeField] private float doubleJumpMultiplier = 0.8f;
    [SerializeField] private bool useDoubleJump;

    private PlayerRefData _playerRefData;
    private MovementProperties _currentMultipliers;
    private Vector3 _lastExternalForce;
    private Vector3 _externalForce;
    private RaycastHit _groundHit;
    private float _inputMagnitude;
    private float _acceleration;
    private float _jumpForce;
    private float _friction;
    private float _maxGravity;
    private float _gravity;
    private float _lastJumpFrame;
    private float _smoothedInputMagnitude;
    private bool _jumpEnabled;
    private bool _moveEnabled;
    private bool _useGravity;
    private bool _useFriction;
    private bool _hasJumped;
    private bool _canDoubleJump;

    public Vector3 WishDir { get; private set; }
    public Rigidbody Rb { get; private set; }
    public PlayerMoveState CurrentMovementState { get; private set; }
    private float GetTargetSpeed() => defaultMaxSpeed * _currentMultipliers.maxSpeedMultiplier * _inputMagnitude;
    public bool CanDoubleJump => _canDoubleJump;
    public float SmoothedInputMagnitude => _smoothedInputMagnitude;
    public event Action OnDoubleJump;
    public event Action OnJump;

    private StudioEventEmitter playerEmitter;

    #region Unity Functions
    private void Awake()
    {
        Rb = GetComponent<Rigidbody>();
        _playerRefData = GetComponent<PlayerRefData>();
        playerEmitter = GetComponent<StudioEventEmitter>();

        _gravity = 2 * apexHeight / Mathf.Pow(apexTime, 2);
        _jumpForce = 2 * apexHeight / apexTime;
        _maxGravity = _gravity;
        _friction = defaultMaxSpeed / timeToZero;
        _acceleration = defaultMaxSpeed / timeToMaxSpeed;
        CurrentMovementState = PlayerMoveState.InAir;
        _useGravity = true;
        _useFriction = true;
        _jumpEnabled = true;
        _moveEnabled = true;

        _externalForce = Vector3.zero;
    }

    private void Update()
    {
        HandleForward();
        HandleJumpBuffer();
        HandleCoyoteTime();
        HandleJump();
        HandleWalkingSFX();
    }

    private void FixedUpdate()
    {
        HandleMovementState();

        Vector3 relVel = Rb.linearVelocity - _lastExternalForce;

        if (_useGravity)
        {
            HandleGravityRelative(ref relVel);
        }

        if (CurrentMovementState == PlayerMoveState.Swinging)
        {
            _playerRefData.PlayerSwing.HandleSwingMovement(WishDir, ref relVel);
            _playerRefData.PlayerSwing.ConstrainToRope(ref relVel);
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

        Rb.linearVelocity = relVel + _externalForce;
        _lastExternalForce = _externalForce;
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
                _canDoubleJump = false;
                break;

            case PlayerMoveState.InAir:
                _currentMultipliers = _airMultipliers;
                if (!_hasJumped && useDoubleJump) _canDoubleJump = true;
                break;

            case PlayerMoveState.Swinging:
                _currentMultipliers = _swingingMultipliers;
                _hasJumped = false;
                _canDoubleJump = false;
                break;
        }
    }

    private void HandleMovementState()
    {
        if (CurrentMovementState == PlayerMoveState.Grabbing) return;

        if (_playerRefData.LassoTetherController.CurrentLassoState == LassoState.Swinging)
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
        DisableJump(isGrabbing);
        DisableMovement(isGrabbing);

        if (isGrabbing)
        {
            SwitchMovementState(PlayerMoveState.Grabbing);

            _currentMultipliers = MovementProperties.Default;
            _lastExternalForce = Vector3.zero;
            _externalForce = Vector3.zero;
            _smoothedInputMagnitude = 0;
            KillVelocity();
            KillRemainingInput();

            if (_playerRefData.Lasso.SnaredObject != null)
            {
                Vector3 dir = _playerRefData.Lasso.SnaredObject.transform.position - transform.position;
                dir.y = 0;
                _playerRefData.PlayerModelRotationHandler.SetNewRotationDir(Quaternion.LookRotation(dir), true);
            }

        }
        else
        {
            SwitchMovementState(PlayerMoveState.InAir);
            _playerRefData.PlayerModelRotationHandler.SetNewRotationDir(null, false);
        }
    }

    #endregion

    #region Misc

    public void DisableMovement(bool disable)
    {
        _moveEnabled = !disable;
    }

    public void DisableJump(bool disable)
    {
        _jumpEnabled = !disable;
    }

    public void SetDoubleJumpAvailable(bool available)
    {
        _canDoubleJump = available;
    }

    public void KillVelocity()
    {
        Rb.linearVelocity = Vector3.zero;
        Rb.angularVelocity = Vector3.zero;
    }

    public void KillRemainingInput()
    {
        WishDir = Vector3.zero;
        //add anything else input related here if it comes up
    }

    public void InheritPlatformMomentum(Vector3 externalForce)
    {
        _externalForce = externalForce;
        _lastExternalForce = externalForce;
    }

    public void SetExternalForce(Vector3 externalForce)
    {
        _externalForce = externalForce;
    }

    private void HandleWalkingSFX()
    {
        if (WishDir != Vector3.zero && IsGrounded())
        {
            if (!playerEmitter.IsPlaying())
                playerEmitter.Play();
            //AudioManager.Instance.SFXSource7.UnPause();
        }
        else
        {
            playerEmitter.Stop();
            //AudioManager.Instance.SFXSource7.Pause(); //PlaySFX(aManage.Walk, 5, 1);
        }
    }
    #endregion

    #region Slope Handling

    public Vector3 AdjustVelocityToSlope(Vector3 velocity)
    {
        if (IsGrounded() != null)
        {
            float speed = velocity.magnitude;
            Vector3 projectedDir = Vector3.ProjectOnPlane(velocity.normalized, _groundHit.normal).normalized;
            return projectedDir * speed;
        }
        return velocity;
    }

    private float GetSlopeSurfaceSpeed(Vector3 vel)
    {
        //projects velocity along slope surface
        Vector3 onPlane = Vector3.ProjectOnPlane(vel, _groundHit.normal);
        return onPlane.magnitude;
    }

    private bool IsOnWalkableSlope()
    {
        if (IsGrounded() == null) return false;

        //compare to max slope angle + small buffer to prevent jitter on very slight slopes
        float angle = Vector3.Angle(Vector3.up, _groundHit.normal);
        return angle > 0.5f && angle <= maxSlopeAngle;
    }

    private Vector3 ProjectWishDirOnSlope(Vector3 flatWishDir)
    {
        //project the flat input direction onto slope
        Vector3 projected = Vector3.ProjectOnPlane(flatWishDir, _groundHit.normal);
        return projected.normalized * flatWishDir.magnitude;
    }


    #endregion

    #region Detection

    public Transform IsGrounded()
    {
        if (Physics.SphereCast(transform.position, feetRadius, Vector3.down, out _groundHit, 1.1f, groundLayer))
        {
            PlayerRefData.Instance.PlayerLedgeGrab.ResetGrabCooldown();
            return _groundHit.transform;
        }
        return null;
    }

    #endregion

    #region Forward / Camera
    public void HandleForward()
    {
        if (CurrentMovementState == PlayerMoveState.Grabbing)
        {
            WishDir = Vector3.zero;
            return;
        }

        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;

        camRight.y = 0;
        camRight.Normalize();
        camForward.y = 0;
        camForward.Normalize();

        Vector3 forwardRelative = camForward * PlayerActions.Instance.MoveInput.y;
        Vector3 rightRelative = camRight * PlayerActions.Instance.MoveInput.x;

        Vector3 desiredDir = forwardRelative + rightRelative;
        float rawMagnitude = Mathf.Clamp01(desiredDir.magnitude);
        bool decelerating = rawMagnitude < _smoothedInputMagnitude;
        float duration = decelerating ? timeToZero : timeToMaxSpeed;
        float step = Time.deltaTime / duration;
        _smoothedInputMagnitude = Mathf.MoveTowards(_smoothedInputMagnitude, rawMagnitude, step);
        _inputMagnitude = _smoothedInputMagnitude;
        WishDir = _inputMagnitude > 0.001f ? desiredDir.normalized : Vector3.zero;
    }
    #endregion

    #region Jumping

    private void HandleJumpBuffer()
    {
        if (!_jumpEnabled) return;

        if (PlayerActions.Instance.JumpDown)
        {
            if (_lastJumpFrame == Time.frameCount) return; //to prevent ledge jump -> double jump misfires
            jumpBufferCounter = jumpBufferTime;

        }
        else if (jumpBufferCounter > 0)
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }

    private void HandleCoyoteTime()
    {
        bool grounded = IsGrounded() && Rb.linearVelocity.y <= 0f;

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
            //regular jump
            if (!_hasJumped && coyoteTimeCounter > 0f)
            {
                coyoteTimeCounter = 0;
                jumpBufferCounter = 0;
                //AudioManager.Instance.PlaySFX(AudioManager.Instance.Jump, 6, 1f);
                RuntimeManager.PlayOneShot("event:/Jump", transform.position);

                Jump(1f, true);
            }

            else if (_canDoubleJump && useDoubleJump)
            {
                //Jump(doubleJumpMultiplier);
                HandleDoubleJump();
            }
        }
    }

    public void Jump(float multiplier, bool enableDoubleJump)
    {
        Vector3 jumpForce = Vector3.up * _jumpForce * multiplier;

        Rb.linearVelocity = new Vector3(Rb.linearVelocity.x, 0, Rb.linearVelocity.z);
        Rb.AddForce(jumpForce, ForceMode.Impulse);
        _hasJumped = true;
        _canDoubleJump = enableDoubleJump;
        _lastJumpFrame = Time.frameCount;

        OnJump?.Invoke();
    }

    public void HandleDoubleJump()
    {
        //v2: redirect current horizontal velocity to wishDir, set vertical to jump force
        Vector3 horizontalVel = new Vector3(Rb.linearVelocity.x, 0, Rb.linearVelocity.z);
        float speed = horizontalVel.magnitude;

        Vector3 redirectedVel = horizontalVel;
        if (WishDir.sqrMagnitude > 0)
            redirectedVel = WishDir.normalized * speed;

        //add upward impulse on top of existing Y - discard downward momentum, keep upward
        float currentUpward = Mathf.Max(Rb.linearVelocity.y, 0f);
        float jumpY = _jumpForce * doubleJumpMultiplier;
        float newY = Mathf.Max(currentUpward, jumpY); //never weaken an existing upward arc

        Rb.linearVelocity = new Vector3(redirectedVel.x, newY, redirectedVel.z);
        _canDoubleJump = false;
        RuntimeManager.PlayOneShot("event:/DoubleJump", transform.position);

        OnDoubleJump?.Invoke();
    }
    #endregion

    #region Gravity
    private void HandleGravityRelative(ref Vector3 relVel)
    {
        if (CurrentMovementState == PlayerMoveState.Walking && Rb.linearVelocity.y <= 0.1f) return;

        relVel.y -= _gravity * Time.fixedDeltaTime;
        if (relVel.y < -_maxGravity)
        {
            relVel.y = -_maxGravity;
        }
    }

    public void ApplySlowFall(float multiplier)
    {
        if (Rb.linearVelocity.y < 0)
        {
            _gravity = _maxGravity * multiplier;

            if (Rb.linearVelocity.y <= -_gravity)
            {
                Rb.linearVelocity = new Vector3(Rb.linearVelocity.x, -_gravity, Rb.linearVelocity.z);
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

    #endregion

    #region Velocity Overshoot
    private void HandleVelocityOvershootRelative(ref Vector3 relVel)
    {
        float hardMax = defaultMaxSpeed * hardCapMultiplier;
        float currentMax = _currentMultipliers.maxSpeedMultiplier * defaultMaxSpeed;

        //use velocity on slope or horizontal velocity
        bool onSlope = IsOnWalkableSlope();
        Vector3 horizontalRel = onSlope ? Vector3.ProjectOnPlane(relVel, _groundHit.normal) : new Vector3(relVel.x, 0, relVel.z);
        float speed = horizontalRel.magnitude;

        if (speed > currentMax)
        {
            float t = Mathf.InverseLerp(currentMax, hardMax, speed); //gives 0-1 based on how far between currentMax and hardMax the speed is
            float easedT = Mathf.SmoothStep(0f, 1f, t);
            float decayStrength = overshootMaxForce * easedT;

            if (!IsGrounded()) decayStrength /= 2f;

            float newSpeed = Mathf.MoveTowards(speed, currentMax, decayStrength * Time.fixedDeltaTime);
            Vector3 normalComponent = onSlope ? Vector3.Project(relVel, _groundHit.normal) : new Vector3(0, relVel.y, 0);
            relVel = (horizontalRel / speed) * newSpeed + normalComponent;
        }
    }
    #endregion

    #region Friction
    public void ApplyFriction(ref Vector3 playerVel, Vector3 frictionAxis)
    {
        if (frictionAxis.sqrMagnitude < 0.0001f) return;

        frictionAxis.Normalize();
        Vector3 velocityOnAxis = Vector3.Project(playerVel, frictionAxis);

        if (velocityOnAxis.sqrMagnitude < 0.0001f) return;

        float speed = velocityOnAxis.magnitude;
        float targetSpeed = GetTargetSpeed();
        Vector3 frictionDir = -velocityOnAxis.normalized;

        Vector3 horizontalVel = new Vector3(playerVel.x, 0, playerVel.z);
        bool isAboveMaxSpeed = horizontalVel.magnitude > targetSpeed;

        float frictionAccel;

        //stopping friction -> bring you to a stop
        //also runs a flat friction amount when above max speed (i.e. when launched)
        if (WishDir == Vector3.zero || isAboveMaxSpeed)
        {
            float stopAccel = speed / Time.fixedDeltaTime;
            float rawFriction = _friction * _currentMultipliers.decelMultiplier;
            frictionAccel = Mathf.Min(rawFriction, stopAccel);
        }
        //movement friction -> keep you at maxSpeed
        else
        {
            frictionAccel = _acceleration * _currentMultipliers.accelMultiplier * (speed / targetSpeed);
        }

        float deltaV = frictionAccel * Time.fixedDeltaTime;
        float newSpeed = Mathf.Max(0f, speed - deltaV);

        if (newSpeed < 0.01f) newSpeed = 0f;

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
        if (!_moveEnabled) return;

        bool onSlope = IsOnWalkableSlope();

        //when on slope, project desired direction onto slope plane so you accelerate along the surface instead of into it
        Vector3 moveDir = onSlope ? ProjectWishDirOnSlope(WishDir.normalized) : WishDir.normalized;
        float currentSpeed = onSlope ? GetSlopeSurfaceSpeed(relVel) : new Vector3(relVel.x, 0, relVel.z).magnitude;
        float targetSpeed = GetTargetSpeed();

        if (currentSpeed < targetSpeed)
        {
            Vector3 surfaceVel = onSlope ? Vector3.ProjectOnPlane(relVel, _groundHit.normal) : new Vector3(relVel.x, 0, relVel.z);
            float currentSpeedInWishDir = Vector3.Dot(surfaceVel, moveDir);
            float accelAmount = _acceleration * _currentMultipliers.accelMultiplier * Time.fixedDeltaTime;
            float speedDeficit = targetSpeed - currentSpeedInWishDir;
            float clampedAccel = Mathf.Min(accelAmount, speedDeficit);

            relVel += moveDir * clampedAccel;
        }
        else
        {
            //redirect velocity at max speed
            Vector3 surfaceVel = onSlope ? Vector3.ProjectOnPlane(relVel, _groundHit.normal) : new Vector3(relVel.x, 0, relVel.z);
            Vector3 targetVelocity = moveDir * currentSpeed;
            float steerStrength = _acceleration * _currentMultipliers.accelMultiplier * steeringMultiplier * Time.fixedDeltaTime;
            Vector3 newSurfaceVel = Vector3.MoveTowards(surfaceVel, targetVelocity, steerStrength);

            if (onSlope)
            {
                //keep residual normal force, but redirect on the slope
                Vector3 normalComponent = Vector3.Project(relVel, _groundHit.normal);
                relVel = newSurfaceVel + normalComponent;
            }
            else
            {
                relVel.x = newSurfaceVel.x;
                relVel.z = newSurfaceVel.z;
            }
        }
    }
    #endregion
}