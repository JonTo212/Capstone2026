using System.Collections;
using UnityEngine;

public class PlayerModelRotationHandler : MonoBehaviour
{
    public enum RotationState
    {
        Default,
        Frozen
    }

    [SerializeField] private GameObject playerObj;
    [SerializeField] private float rotationSpeed;

    private PlayerRefData _playerRefData;
    private RotationState _currentRotationState;
    private Quaternion desiredRot;

    void Awake()
    {
        _playerRefData = GetComponent<PlayerRefData>();
    }

    void Update()
    {
        if (_playerRefData.PlayerMovement.WishDir == Vector3.zero) return;
        if (_currentRotationState == RotationState.Frozen) return;

        playerObj.transform.rotation = Quaternion.Slerp(playerObj.transform.rotation, desiredRot, rotationSpeed * Time.deltaTime);
        desiredRot = Quaternion.LookRotation(_playerRefData.PlayerMovement.WishDir);
    }

    public void SetNewRotationDir(Quaternion? desiredRotation, bool hanging)
    {
        if (desiredRotation != null)
        {
            playerObj.transform.rotation = desiredRotation.Value;
            desiredRot = desiredRotation.Value;
        }
        if (hanging) _currentRotationState = RotationState.Frozen;
        else _currentRotationState = RotationState.Default;
    }

    //applies a Z-roll sway onto an explicitly provided world-space yaw
    //caller must supply the yaw so we never read back world eulerAngles (which can flip)
    //must be called BEFORE SetLeanAngle each frame so the lean stacks on top
    public void SetSwayAngle(float worldYaw, float swayAngle)
    {
        playerObj.transform.rotation = Quaternion.Euler(0f, worldYaw, -swayAngle);
    }

    //applies a forward X-pitch on top of the current rotation (including any sway).
    //call AFTER SetSwayAngle each frame so the lean stacks on top of the roll.
    public void SetLeanAngle(float angle)
    {
        playerObj.transform.rotation *= Quaternion.Euler(angle, 0f, 0f);
    }
}