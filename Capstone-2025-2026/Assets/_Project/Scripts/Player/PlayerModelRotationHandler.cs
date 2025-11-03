using UnityEngine;

public class PlayerModelRotationHandler : MonoBehaviour
{
    [SerializeField] private GameObject playerObj;
    [SerializeField] private float rotationSpeed;

    private PlayerMovement _playerController;

    void Awake()
    {
        _playerController = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        if (_playerController.WishDir == Vector3.zero) return;

        Quaternion desiredRot = Quaternion.LookRotation(_playerController.WishDir);
        playerObj.transform.rotation = Quaternion.Slerp(playerObj.transform.rotation, desiredRot, rotationSpeed * Time.deltaTime);
    }
}
