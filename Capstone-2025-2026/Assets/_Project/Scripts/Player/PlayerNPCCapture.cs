using System;
using System.Collections;
using UnityEngine;

public class PlayerNPCCapture : MonoBehaviour
{
    [SerializeField] private Transform npcBackpackPos;
    private PlayerRefData _playerRefData;

    [Header("Object Yank Properties")]
    [SerializeField] private float handAttachThreshold = 0.2f;
    [SerializeField] private float objectYankDuration = 0.5f;
    private Coroutine _objectYankCoroutine;

    public event Action OnObjectYankCompleted;

    private void Start()
    {
        _playerRefData = GetComponent<PlayerRefData>();
        _playerRefData.Lasso.OnNPCHit += CaptureNPC;
    }

    private void OnDisable()
    {
        _playerRefData.Lasso.OnNPCHit -= CaptureNPC;
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
        if (_objectYankCoroutine != null)
            StopCoroutine(_objectYankCoroutine);

        _objectYankCoroutine = StartCoroutine(YankObjectCoroutine(_playerRefData.Lasso.SnaredObject.transform, _playerRefData.Lasso.SnaredObject.transform, npcBackpackPos));
        PlayerActions.Instance.DisableAllInput();

        Vector3 toTarget = (_playerRefData.Lasso.SnaredObject.transform.position - transform.position).normalized;
        toTarget.y = 0f;
        Quaternion targetRotation = Quaternion.LookRotation(toTarget, Vector3.up);
        _playerRefData.PlayerModelRotationHandler.SetNewRotationDir(targetRotation, false);

        PickupNPCProp pickupObj = _playerRefData.Lasso.SnaredObject as PickupNPCProp;
        pickupObj.OnCaptureStart(objectYankDuration);
    }

    private IEnumerator YankObjectCoroutine(Transform yankObj, Transform startPos, Transform endPos)
    {
        Prop prop = yankObj.GetComponent<Prop>();
        if (prop == null) yield break;
        yankObj.GetComponent<Rigidbody>().isKinematic = false;

        float attachThreshold = handAttachThreshold;
        prop.GetComponent<Collider>().enabled = false;
        prop.Rb.constraints = RigidbodyConstraints.None;
        prop.Rb.useGravity = true;

        Vector3 toPlayer = endPos.position - startPos.position;
        float startTime = Time.time;

        //apply initial velocity
        Vector3 startVel = CalculateObjectYankVelocity(startPos.position, endPos.position, objectYankDuration);
        prop.Rb.linearVelocity = Vector3.zero;
        prop.Rb.angularVelocity = Vector3.zero;
        prop.Rb.AddForce(startVel * prop.Rb.mass, ForceMode.Impulse);

        while (Vector3.Distance(yankObj.position, endPos.position) > handAttachThreshold)
        {
            if (yankObj == null) break;

            //calculate correctional pull velocity
            float elapsedTime = Time.time - startTime;
            float remainingTime = objectYankDuration - elapsedTime;
            float clampedRemainingTime = Mathf.Max(remainingTime, 0.0125f);

            Vector3 idealVelocity = CalculateObjectYankVelocity(yankObj.position, endPos.position, clampedRemainingTime);
            Vector3 velocityError = idealVelocity - prop.Rb.linearVelocity;

            prop.Rb.AddForce(velocityError * prop.Rb.mass, ForceMode.Impulse);


            //calculate correctional torque
            Quaternion targetRotation = Quaternion.LookRotation(-transform.forward, Vector3.up);
            Quaternion deltaRotation = targetRotation * Quaternion.Inverse(yankObj.rotation);

            deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;

            Vector3 angularVelocity = axis * angle * Mathf.Deg2Rad / clampedRemainingTime;
            Vector3 angularError = angularVelocity - prop.Rb.angularVelocity;

            prop.Rb.AddTorque(angularError, ForceMode.VelocityChange);

            yield return new WaitForFixedUpdate();
        }
        
        OnObjectYankCompleted?.Invoke();
        _objectYankCoroutine = null;
        PlayerActions.Instance.EnableAllInput();
        _playerRefData.LassoTetherController.ClearHold();
        PlayerActions.Instance.RumbleFor(0.2f, 0.4f, 0.1f);
        prop.gameObject.SetActive(false);
    }

    #endregion

    #region NPC Handling

    private void CaptureNPC()
    {
        HandleObjectYank();
    }

    #endregion
}
