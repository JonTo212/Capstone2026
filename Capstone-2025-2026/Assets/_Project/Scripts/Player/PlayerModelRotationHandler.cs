using System.Collections;
using UnityEngine;

public class PlayerModelRotationHandler : MonoBehaviour
{
    public enum RotationState
    {
        Default,
        WallJump
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
        if (_currentRotationState == RotationState.WallJump) return;

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

    public void SetNewRotationDir(Quaternion desiredRotation)
    {
        playerObj.transform.rotation = desiredRotation;
        desiredRot = desiredRotation;
    }
}
