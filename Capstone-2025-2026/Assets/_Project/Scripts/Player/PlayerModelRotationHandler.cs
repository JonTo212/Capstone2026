using System.Collections;
using UnityEngine;

public class PlayerModelRotationHandler : MonoBehaviour
{
    public enum RotationState
    {
        Default,
        Hanging
    }

    [SerializeField] private GameObject playerObj;
    [SerializeField] private float rotationSpeed;

    private PlayerMovement _playerController;
    private RotationState _currentRotationState;
    private Coroutine _lockRotationCoroutine;
    Quaternion desiredRot;

    void Awake()
    {
        _playerController = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        if (_playerController.WishDir == Vector3.zero) return;
        if (_currentRotationState == RotationState.Hanging) return;

        playerObj.transform.rotation = Quaternion.Slerp(playerObj.transform.rotation, desiredRot, rotationSpeed * Time.deltaTime);
        desiredRot = Quaternion.LookRotation(_playerController.WishDir);
    }

    /*public void SetNewRotationDir(Vector3? dir, float lockDuration)
    {
        if (dir == null || lockDuration == 0)
        {
            if(_lockRotationCoroutine != null) StopCoroutine(_lockRotationCoroutine);
            _currentRotationState = RotationState.Default;
            return;
        }

        playerObj.transform.rotation = Quaternion.LookRotation(dir.Value);
        /*
        if (dir == null || lockDuration == 0)
        {
            StopCoroutine(_lockRotationCoroutine);
            _currentRotationState = RotationState.Default;
            return;
        }

        desiredRot = Quaternion.LookRotation(dir.Value);
        if (_lockRotationCoroutine != null)
        {
            StopCoroutine(_lockRotationCoroutine);
        }
        _lockRotationCoroutine = StartCoroutine(RotationLockTimer(lockDuration));
    }

    private IEnumerator RotationLockTimer(float duration)
    {
        _currentRotationState = RotationState.WallJump;
        _playerController.PlayerInput.ChangeSpecificInput("Move", false);

        float lockTimer = 0;

        while(lockTimer < duration)
        {
            playerObj.transform.rotation = Quaternion.Slerp(playerObj.transform.rotation, desiredRot, rotationSpeed * 2f * Time.deltaTime);

            lockTimer += Time.deltaTime;
            yield return null;
        }

        _playerController.PlayerInput.ChangeSpecificInput("Move", true);
        _currentRotationState = RotationState.Default;
    }
    }*/

    public void SetNewRotationDir(Quaternion? desiredRotation, bool hanging)
    {
        if (desiredRotation != null) playerObj.transform.rotation = desiredRotation.Value;
        if (hanging) _currentRotationState = RotationState.Hanging;
        else _currentRotationState = RotationState.Default;
    }

    // Applies a Z-roll sway onto an explicitly provided world-space yaw.
    // Caller must supply the yaw so we never read back world eulerAngles (which can flip).
    // Must be called BEFORE SetLeanAngle each frame so the lean stacks on top.
    public void SetSwayAngle(float worldYaw, float swayAngle)
    {
        playerObj.transform.rotation = Quaternion.Euler(0f, worldYaw, -swayAngle);
    }

    // Applies a forward X-pitch on top of the current rotation (including any sway).
    // Call AFTER SetSwayAngle each frame so the lean stacks on top of the roll.
    public void SetLeanAngle(float angle)
    {
        playerObj.transform.rotation *= Quaternion.Euler(angle, 0f, 0f);
    }
}