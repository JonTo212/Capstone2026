using System.Collections;
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
    [SerializeField] private float overshootCorrectionMultiplier = 10f;
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

    [Header("Camera")]
    [SerializeField] private Camera playerCam;

    [Header("Input")]
    private PlayerActions _playerActions;
    private PlayerSwing _playerSwing;
    private LassoTetherController _lassoTetherController;
    private PlayerWallBounce _playerWallBounce;

    private MovementProperties _currentMultipliers;
    private PlayerMoveState _currentMovementState;
    private Vector3 _wishDir;
    private Rigidbody _rb;
    private CapsuleCollider _playerCol;
    private Coroutine _externalForceRoutine;
    private float _acceleration;
    private float _gravity;
    private float _jumpForce;
    private float _friction;
    private float _defaultFOV;
    private float _maxGravity;
    private bool _useGravity;
    private bool _useFriction;

    public Vector3 ExternalForce { get; set; }
    public Vector3 PlayerVelocity { get; set; }
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
        _playerWallBounce = GetComponent<PlayerWallBounce>();

        _gravity = 2 * apexHeight / Mathf.Pow(apexTime, 2);
        _jumpForce = 2 * apexHeight / apexTime;
        _maxGravity = _gravity;
        _friction = defaultMaxSpeed / timeToZero;
        _acceleration = defaultMaxSpeed / timeToMaxSpeed;
        _defaultFOV = playerCam.fieldOfView;
        _currentMovementState = PlayerMoveState.InAir;
        _useGravity = true;
        _useFriction = true;

        ExternalForce = Vector3.zero;

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

    private Vector3 _lastExternalForce;
    private void FixedUpdate()
    {
        HandleMovementState();

        if (_currentMovementState == PlayerMoveState.Grabbing)
        {
            return;
        }

        HandleForward();
        Vector3 relVel = _rb.linearVelocity - _lastExternalForce;

        if (_useGravity)
        {
            HandleGravityRelative(ref relVel);
        }

        if (_currentMovementState == PlayerMoveState.Swinging)
        {
            _playerSwing.HandleSwingMovement(_wishDir, ref relVel);
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

        _rb.linearVelocity = relVel + ExternalForce;
        _lastExternalForce = ExternalForce;
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

    public void SetGrabbing(bool isGrabbing)
    {
        if (isGrabbing) SwitchMovementState(PlayerMoveState.Grabbing);
        else SwitchMovementState(PlayerMoveState.InAir);
    }

    public void InheritPlatformMomentum(Vector3 externalForce)
    {
        ExternalForce = externalForce;
        EaseExternalForceToZero(1f);
    }

    private void EaseExternalForceToZero(float duration)
    {
        if (_externalForceRoutine != null)
            StopCoroutine(_externalForceRoutine);

        _externalForceRoutine = StartCoroutine(EaseExternalForceRoutine(duration));
    }

    private IEnumerator EaseExternalForceRoutine(float duration)
    {
        Vector3 startForce = ExternalForce;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / duration);
            ExternalForce = startForce * alpha;
            yield return null;
        }

        ExternalForce = Vector3.zero;
        _externalForceRoutine = null;
    }

    public bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 1.1f, groundLayer);
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

    public void EnableFriction(bool enable)
    {
        _useFriction = enable;
    }

    private void HandleMovementState()
    {
        if (_currentMovementState == PlayerMoveState.Grabbing) return;

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
        if (jumpBufferCounter > 0)
        {
            //wall bounce
            if (_playerWallBounce.TryWallBounce())
            {
                jumpBufferCounter = 0;
                AudioManager.Instance.PlaySFX(AudioManager.Instance.Jump, 6, 1f);
                _rb.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
                return;
            }
            
            //regular jump
            if (coyoteTimeCounter > 0)
            {
                coyoteTimeCounter = 0f;
                jumpBufferCounter = 0;
                AudioManager.Instance.PlaySFX(AudioManager.Instance.Jump, 6, 1f);

                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
                _rb.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
            }
        }
    }

    private void HandleGravityRelative(ref Vector3 relVel)
    {
        if (_currentMovementState == PlayerMoveState.Walking) return;

        //apply gravity to the reference variable instead of addForce
        relVel.y -= _gravity * Time.fixedDeltaTime;
        if (relVel.y < -_maxGravity)
        {
            relVel.y = -_maxGravity;
        }
    }

    private void HandleVelocityOvershootRelative(ref Vector3 relVel)
    {
        float currentMax = _currentMultipliers.maxSpeedMultiplier * defaultMaxSpeed;
        float hardCap = currentMax * hardCapMultiplier;

        // Check horizontal relative speed
        Vector3 horizontalRel = new Vector3(relVel.x, 0, relVel.z);
        float speed = horizontalRel.magnitude;

        if (speed > currentMax)
        {
            float overshoot = speed - currentMax;
            float t = Mathf.Clamp01(overshoot / (hardCap - currentMax));
            float correctionAmount = overshoot * t * overshootCorrectionMultiplier * Time.fixedDeltaTime;

            relVel -= horizontalRel.normalized * Mathf.Min(correctionAmount, overshoot);
        }
    }


    public void ApplyFriction(ref Vector3 playerVel, Vector3 frictionAxis)
    {
        frictionAxis.Normalize();
        Vector3 velocityOnAxis = Vector3.Project(playerVel, frictionAxis);
        float speed = velocityOnAxis.magnitude;

        if (speed <= 0f)
            return;

        Vector3 frictionDir = -velocityOnAxis.normalized;

        float frictionAccel;

        if (_wishDir == Vector3.zero)
        {
            float stopAccel = speed / Time.fixedDeltaTime;
            float rawFriction = _friction * _currentMultipliers.decelMultiplier;
            frictionAccel = Mathf.Min(rawFriction, stopAccel);
        }
        else
        {
            float targetSpeed = defaultMaxSpeed * _currentMultipliers.maxSpeedMultiplier;
            frictionAccel = _acceleration * _currentMultipliers.accelMultiplier * (speed / targetSpeed);
        }

        float deltaV = frictionAccel * Time.fixedDeltaTime;
        float newSpeed = Mathf.Max(0f, speed - deltaV);
        playerVel = (playerVel - velocityOnAxis) + (velocityOnAxis.normalized * newSpeed);
    }


    private void ApplyAccelerationRelative(ref Vector3 relVel)
    {
        if (_wishDir == Vector3.zero) return;

        //apply accel to reference (same as Forcemode.Accel)
        Vector3 accel = _wishDir * _acceleration * _currentMultipliers.accelMultiplier;
        relVel += accel * Time.fixedDeltaTime;
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
