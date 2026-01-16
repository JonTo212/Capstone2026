using UnityEngine;

public class PlayerWallBounce : MonoBehaviour
{
    [SerializeField] private float wallBounceForce = 10f;
    [SerializeField] private float wallCheckDistance = 1f;
    [SerializeField] private float minimumHeight = 1f;
    [SerializeField] private LayerMask wallMask;
    [SerializeField] private Transform forwardRef;

    private Vector3 _lastWallNormal;
    private PlayerMovement _playerController;


    private void Awake()
    {
        _playerController = GetComponent<PlayerMovement>();
    }

    private bool CheckWall(out Vector3 wallNormal)
    {
        //check everything around the player
        wallNormal = Vector3.zero;
        Collider[] hits = Physics.OverlapSphere(transform.position, wallCheckDistance, wallMask);

        if (hits.Length == 0)
            return false;

        //get closest wall
        Collider closest = null;
        float closestDist = float.MaxValue;

        foreach (var col in hits)
        {
            float dist = Vector3.Distance(transform.position, col.ClosestPoint(transform.position));
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = col;
            }
        }

        if (closest == null)
            return false;

        Vector3 closestPoint = closest.ClosestPoint(transform.position);
        wallNormal = (transform.position - closestPoint).normalized;

        //filter ground, anything under 5 degrees is considered ground
        float angle = Vector3.Angle(wallNormal, Vector3.up);
        if (angle < 5f)
            return false;

        return true;
    }


    private bool CheckHeight()
    {
        return !Physics.Raycast(transform.position, Vector3.down, minimumHeight);
    }

    public bool TryWallBounce()
    {
        if (!CheckHeight()) return false;
        if (!CheckWall(out _lastWallNormal)) return false;

        Vector3 normal = _lastWallNormal;
        Vector3 jumpAccel = normal * wallBounceForce;

        Vector3 vel = _playerController.Rb.linearVelocity;
        CancelOpposingVelocity(ref vel, jumpAccel);
        _playerController.Rb.linearVelocity = vel;

        _playerController.Rb.AddForce(jumpAccel, ForceMode.Impulse);
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
