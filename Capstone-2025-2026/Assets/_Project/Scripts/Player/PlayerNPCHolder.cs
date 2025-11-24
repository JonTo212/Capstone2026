using System;
using System.Collections;
using UnityEngine;

public class PlayerNPCHolder : MonoBehaviour
{
    public INPC currentNPC { get; set; }
    private PlayerActions _playerInput;
    private Lasso _playerLasso;
    private Coroutine _objectYankCoroutine;

    [Header("Object Yank Properties")]
    [SerializeField] private float handAttachThreshold = 0.2f;
    [SerializeField] private float objectYankDuration = 0.5f;
    [SerializeField] private Transform holdPos;

    public event Action OnObjectYankCompleted;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerActions>();
        _playerLasso = GetComponent<Lasso>();

        _playerLasso.OnNPCHit += SetConnectedNPC;
    }

    private void Update()
    {
        if(currentNPC != null)
        {
            if(_playerInput.JumpDown)
            {
               currentNPC.SwitchNPCState(NPCState.UsingAbility);
            }
            if (_playerInput.JumpUp)
            {
                _playerLasso.PlayerController.ResetGravity();
                currentNPC.SwitchNPCState(NPCState.Attached);
            }
        }
    }

    #region Helper Functions

    private Vector3 CalculateObjectYankVelocity(Vector3 start, Vector3 end, float flightTime) //no control over Y (projectile motion)
    {
        //initial vel = (total displacement - (1/2(accel * time)^2)) / time)
        Vector3 displacement = end - start;
        float gravity = Physics.gravity.y;

        Vector3 velocityXZ = new Vector3(displacement.x / flightTime, 0f, displacement.z / flightTime);
        float velocityY = (displacement.y - 0.5f * gravity * flightTime * flightTime) / flightTime;

        return velocityXZ + Vector3.up * velocityY;
    }

    #endregion

    #region Object Yank
    public void HandleObjectYank()
    {
        if (!_playerLasso.SnaredObject.transform.TryGetComponent(out INPC npc)) return;
        else
        {
            _playerLasso.SnaredObject.transform.GetComponent<Collider>().enabled = false;

            if (_objectYankCoroutine != null)
            {
                StopCoroutine(_objectYankCoroutine);
            }

            _objectYankCoroutine = StartCoroutine(YankObjectCoroutine());
        }
    }

    private IEnumerator YankObjectCoroutine()
    {
        _playerLasso.SnaredObject.Rb.useGravity = true;
        _playerLasso.SnaredObject.Rb.linearDamping = 0f;

        Vector3 startPosition = _playerLasso.SnaredObject.transform.position;
        Vector3 toPlayer = holdPos.position - startPosition;
        float startTime = Time.time;

        //apply initial velocity
        AudioManager.Instance.PlaySFX(AudioManager.Instance.Pull, 5, 1);
        Vector3 startVel = CalculateObjectYankVelocity(startPosition, holdPos.position, objectYankDuration);
        _playerLasso.SnaredObject.Rb.linearVelocity = Vector3.zero;
        _playerLasso.SnaredObject.Rb.AddForce(startVel * _playerLasso.SnaredObject.Rb.mass, ForceMode.Impulse);


        while (Vector3.Distance(_playerLasso.SnaredObject.transform.position, holdPos.position) > handAttachThreshold)
        {
            if (_playerLasso.SnaredObject == null) break;

            //calculate correctional pull velocity
            float elapsedTime = Time.time - startTime;
            float remainingTime = objectYankDuration - elapsedTime;
            float clampedRemainingTime = Mathf.Max(remainingTime, 0.05f);

            Vector3 idealVelocity = CalculateObjectYankVelocity(_playerLasso.SnaredObject.transform.position, holdPos.position, clampedRemainingTime);
            Vector3 velocityError = idealVelocity - _playerLasso.SnaredObject.Rb.linearVelocity;

            _playerLasso.SnaredObject.Rb.AddForce(velocityError * _playerLasso.SnaredObject.Rb.mass, ForceMode.Impulse);


            //calculate correctional torque
            Quaternion targetRotation = Quaternion.LookRotation(holdPos.forward, Vector3.up);
            Quaternion deltaRotation = targetRotation * Quaternion.Inverse(_playerLasso.SnaredObject.transform.rotation);

            deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;

            Vector3 angularVelocity = axis * angle * Mathf.Deg2Rad / clampedRemainingTime;
            Vector3 angularError = angularVelocity - _playerLasso.SnaredObject.Rb.angularVelocity;

            _playerLasso.SnaredObject.Rb.AddTorque(angularError, ForceMode.VelocityChange);


            yield return new WaitForFixedUpdate();
        }

        if (_playerLasso.SnaredObject != null)
        {
            _playerLasso.SnaredObject.OnHold(holdPos);
            _playerLasso.SnaredObject.AttachedTransform = transform;
        }

        OnObjectYankCompleted?.Invoke();
        _objectYankCoroutine = null;
    }

    #endregion

    #region NPC Handling

    private void SetConnectedNPC()
    {
        INPC npc = _playerLasso.SnaredObject as INPC;
        currentNPC = npc;
        npc.AttachObject(transform);
        npc.SwitchNPCState(NPCState.Disturbed);
    }

    #endregion
}
