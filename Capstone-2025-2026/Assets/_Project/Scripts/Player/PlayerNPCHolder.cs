using System;
using System.Collections;
using UnityEngine;

public class PlayerNPCHolder : MonoBehaviour
{
    public INPC currentNPC { get; set; }
    [field: SerializeField] public Transform npcRemovePos { get; private set; }
    private PlayerActions _playerInput;
    private Lasso _playerLasso;
    private Coroutine _objectYankCoroutine;
    private Transform connectedNPC;

    [Header("Object Yank Properties")]
    [SerializeField] private float handAttachThreshold = 0.2f;
    [field: SerializeField] public float objectYankDuration { get; private set; } = 0.5f;
    [SerializeField] private Transform holdPos;

    public event Action OnObjectYankCompleted;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerActions>();
        _playerLasso = GetComponent<Lasso>();

        _playerLasso.OnNPCHit += SetConnectedNPC;
    }

    private void OnDisable()
    {
        _playerLasso.OnNPCHit -= SetConnectedNPC;
    }

    private void Update()
    {
        if(currentNPC != null)
        {
            if(_playerInput.JumpHeld)
            {
                currentNPC.UseAbility();
            }
            if (_playerInput.JumpUp)
            {
                currentNPC.StopAbility();
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
        if(currentNPC != null)
        {
            if (currentNPC.CurrentNPCState != NPCState.InBag)
            {
                connectedNPC.GetComponent<Collider>().enabled = false;

                if (_objectYankCoroutine != null)
                    StopCoroutine(_objectYankCoroutine);

                _objectYankCoroutine = StartCoroutine(YankObjectCoroutine(connectedNPC, connectedNPC, holdPos, true));
                currentNPC.OnCaptureStart();
            }
            else
            {
                if (_objectYankCoroutine != null)
                    StopCoroutine(_objectYankCoroutine);

                _objectYankCoroutine = StartCoroutine(YankObjectCoroutine(connectedNPC, holdPos, npcRemovePos, false));
                currentNPC.OnReleaseStart();
            }
        }
    }

    private IEnumerator YankObjectCoroutine(Transform yankObj, Transform startPos, Transform endPos, bool attach)
    {
        Prop prop = yankObj.GetComponent<Prop>();
        if (prop == null) yield break;

        float attachThreshold = handAttachThreshold;
        Vector3 staticEndPos = endPos.position;
        if (!attach)
        {
            attachThreshold *= 5f;
            prop.OnRelease();
        }
        prop.Rb.constraints = RigidbodyConstraints.None;
        prop.Rb.useGravity = true;

        Vector3 startPosition = startPos.position;
        Vector3 toPlayer = endPos.position - startPosition;
        float startTime = Time.time;

        //apply initial velocity
        AudioManager.Instance.PlaySFX(AudioManager.Instance.Pull, 5, 1);
        Vector3 startVel = CalculateObjectYankVelocity(startPosition, attach ? endPos.position : staticEndPos, objectYankDuration);
        prop.Rb.linearVelocity = Vector3.zero;
        prop.Rb.angularVelocity = Vector3.zero;
        prop.Rb.AddForce(startVel * prop.Rb.mass, ForceMode.Impulse);


        while (Vector3.Distance(yankObj.position, attach ? endPos.position : staticEndPos) > handAttachThreshold)
        {
            if (yankObj == null) break;

            //calculate correctional pull velocity
            float elapsedTime = Time.time - startTime;
            float remainingTime = objectYankDuration - elapsedTime;
            float clampedRemainingTime = Mathf.Max(remainingTime, 0.0125f);

            Vector3 idealVelocity = CalculateObjectYankVelocity(yankObj.position, attach ? endPos.position : staticEndPos, clampedRemainingTime);
            Vector3 velocityError = idealVelocity - prop.Rb.linearVelocity;

            prop.Rb.AddForce(velocityError * prop.Rb.mass, ForceMode.Impulse);


            //calculate correctional torque
            Quaternion targetRotation = Quaternion.LookRotation(endPos.forward, Vector3.up);
            Quaternion deltaRotation = targetRotation * Quaternion.Inverse(yankObj.rotation);

            deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;

            Vector3 angularVelocity = axis * angle * Mathf.Deg2Rad / clampedRemainingTime;
            Vector3 angularError = angularVelocity - prop.Rb.angularVelocity;

            prop.Rb.AddTorque(angularError, ForceMode.VelocityChange);

            yield return new WaitForFixedUpdate();
        }

        if (attach)
        {
            prop.OnHold(holdPos);
            prop.AttachedTransform = transform;
            currentNPC.OnCaptureComplete();
        }
        else
        {
            prop.Rb.useGravity = false;
            prop.Rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            ReleaseNPC();
            currentNPC.OnReleaseComplete();
        }

        OnObjectYankCompleted?.Invoke();
        _objectYankCoroutine = null;
    }

    #endregion

    #region NPC Handling

    private void SetConnectedNPC()
    {
        currentNPC = _playerLasso.SnaredObject as INPC;
        currentNPC.SetPlayerRef(transform);
        connectedNPC = _playerLasso.SnaredObject.transform;
    }

    private void ReleaseNPC()
    {
        if (currentNPC == null || _objectYankCoroutine != null) return;

        currentNPC.SetPlayerRef(null);
        connectedNPC = null;
        currentNPC = null;
    }

    #endregion
}
