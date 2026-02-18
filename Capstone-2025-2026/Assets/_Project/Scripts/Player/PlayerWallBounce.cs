using UnityEngine;

public class PlayerWallBounce : MonoBehaviour
{
    //[SerializeField] private float wallBounceForce = 10f;
    [SerializeField] private float wallCheckDistance = 1f;
    [SerializeField] private float minimumHeight = 1f;
    [SerializeField] private LayerMask wallMask;
    [SerializeField] private Transform forwardRef;
    [SerializeField] private float wallAngleThreshold = 10f;
    [SerializeField] private Vector2 wallBounceForce;
    [SerializeField] private float wallJumpDuration;

    private Vector3 _lastWallNormal;
    private PlayerMovement _playerController;
    private PlayerModelRotationHandler _playerModelRotationHandler;


    private void Awake()
    {
        _playerController = GetComponent<PlayerMovement>();
        _playerModelRotationHandler = GetComponent<PlayerModelRotationHandler>();
    }

    private void Update()
    {
        if(CheckWall())
        {
            _playerController.ApplySlowFall(0.25f);
        }
        else
        {
            _playerController.ResetGravity();
        }
    }


    private bool CheckWall()
    {
        Vector3 checkDir = forwardRef.forward;

        float radius = 0.3f;

        if (Physics.SphereCast(transform.position, radius, checkDir, out RaycastHit sphereHit, wallCheckDistance, wallMask))
        {
            _lastWallNormal = sphereHit.normal;
            float wallThreshold = Mathf.Sin(wallAngleThreshold * Mathf.Deg2Rad);
            float verticalDot = Vector3.Dot(_lastWallNormal, Vector3.up);

            if (Mathf.Abs(verticalDot) <= wallThreshold)
            {
                return true;
            }
        }
        return false;
    }


    private bool CheckHeight()
    {
        return !Physics.Raycast(transform.position, Vector3.down, minimumHeight);
    }

    public bool TryWallBounce()
    {
        if (!CheckHeight()) return false;
        if (!CheckWall()) return false;

        Vector3 jumpAccel = (_lastWallNormal * wallBounceForce.x) + (Vector3.up * wallBounceForce.y);

        Vector3 vel = _playerController.Rb.linearVelocity;
        CancelOpposingVelocity(ref vel, jumpAccel);
        vel.y = 0;
        _playerController.Rb.linearVelocity = vel;

        _playerController.Rb.AddForce(jumpAccel, ForceMode.Impulse);
        //_playerController.PlayerModelRotationHandler.SetNewRotationDir(_lastWallNormal, wallJumpDuration);
        return true;
    }

    private void CancelOpposingVelocity(ref Vector3 velocity, Vector3 jumpAccel)
    {
        Vector3 jumpDir = jumpAccel.normalized;
        float dot = Vector3.Dot(velocity, jumpDir);

        if (dot < 0f)
        {
            Vector3 opposingComponent = jumpDir * dot;
            velocity -= opposingComponent;
        }
    }
}
