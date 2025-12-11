using System;
using System.Collections;
using UnityEngine;

public class PlayerMantle : MonoBehaviour
{
    [SerializeField] private float forwardCheckDistance = 1f;
    [SerializeField] private float verticalCheckDistance = 1f;
    [SerializeField] private float climbHeightBuffer = 0.5f;
    [SerializeField] private float forwardClimbBuffer = 0.5f;
    [SerializeField] private float climbDuration = 0.3f;
    [SerializeField] private float forwardDuration = 0.2f;
    [SerializeField] private Transform forwardRef;
    [SerializeField] private LayerMask mantleableLayers;

    private CapsuleCollider _playerCol;
    private PlayerActions _playerActions;
    private PlayerController _playerController;
    private Coroutine _mantleCoroutine;

    public Coroutine MantleCoroutine => _mantleCoroutine;

    public event Action<bool> OnMantle;

    private void Awake()
    {
        _playerCol = GetComponent<CapsuleCollider>();
        _playerActions = GetComponent<PlayerActions>();
        _playerController = GetComponent<PlayerController>();

        if (forwardRef == null) forwardRef = Camera.main.transform;
    }

    private void Update()
    {
        if (_playerActions.JumpHeld && _mantleCoroutine == null)
        {
            Vector3? mantleTarget = TryStartMantle();
            if (mantleTarget.HasValue)
            {
                _mantleCoroutine = StartCoroutine(Mantle(mantleTarget.Value));
            }
        }
    }

    private Vector3? TryStartMantle()
    {
        if (Physics.Raycast(transform.position, forwardRef.forward, out RaycastHit forwardHit, forwardCheckDistance, mantleableLayers))
        {
            float secondCheckDist = _playerCol.height;
            Vector3 secondCheckStartPos = forwardHit.point + (forwardRef.forward * _playerCol.radius) + (Vector3.up * verticalCheckDistance * secondCheckDist);

            if (Physics.Raycast(secondCheckStartPos, Vector3.down, out RaycastHit topHit, secondCheckDist, mantleableLayers))
            {
                Vector3 upOffset = Vector3.up * (_playerCol.height * 0.5f);
                Vector3 backOffset = -forwardRef.forward * _playerCol.radius + forwardRef.forward * forwardClimbBuffer;
                Vector3 target = topHit.point + upOffset + backOffset;

                return target;
            }
        }
        return null;
    }

    private IEnumerator Mantle(Vector3 targetPos)
    {
        _playerController.Rb.isKinematic = true;
        OnMantle?.Invoke(true);

        Vector3 startPos = transform.position;
        Vector3 climbPos = new Vector3(startPos.x, targetPos.y + climbHeightBuffer, startPos.z);

        //first half -> climb upwards
        float timer = 0;
        while (timer < climbDuration)
        {
            timer += Time.fixedDeltaTime;
            float t = timer / climbDuration;
            _playerController.Rb.MovePosition(Vector3.Lerp(startPos, climbPos, t));
            yield return new WaitForFixedUpdate();
        }

        //second half -> forward component
        timer = 0;
        while (timer < forwardDuration)
        {
            timer += Time.fixedDeltaTime;
            float t = timer / forwardDuration;
            _playerController.Rb.MovePosition(Vector3.Lerp(climbPos, targetPos, t));
            yield return new WaitForFixedUpdate();
        }

        _playerController.Rb.position = targetPos;
        _playerController.Rb.isKinematic = false;
        _mantleCoroutine = null;

        OnMantle?.Invoke(false);
    }

}
