using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class PlayerNPCHolder : MonoBehaviour
{
    public INPC CurrentNPC { get; private set; }
    [field: SerializeField] public Transform npcRemovePos { get; private set; }
    [field: SerializeField] public Transform npcBackpackPos { get; private set; }
    [field: SerializeField] public Transform npcAbilityPos { get; private set; }

    private PlayerActions _playerInput;
    private PlayerMantle _playerMantle;
    private PlayerMovement _playerMovement;
    private Lasso _playerLasso;

    [Header("Ability Handling")]
    [SerializeField] private LineRenderer abilityLineRenderer;
    [SerializeField] private Transform camTransform;
    private Transform _connectedNPC;
    private Coroutine _abilityCoroutine;
    private bool useAbilityRequested;
    private bool stopAbilityRequested;
    private bool abilityActive;

    [Header("Object Yank Properties")]
    [SerializeField] private float handAttachThreshold = 0.2f;
    [SerializeField] private float objectYankDuration = 0.5f;
    private Coroutine _objectYankCoroutine;

    public event Action OnObjectYankCompleted;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerActions>();
        _playerLasso = GetComponent<Lasso>();
        _playerMovement = GetComponent<PlayerMovement>();
        _playerMantle = GetComponent<PlayerMantle>();

        _playerLasso.OnNPCHit += SetConnectedNPC;
    }

    private void OnDisable()
    {
        _playerLasso.OnNPCHit -= SetConnectedNPC;
    }

    private void Update()
    {
        if(CurrentNPC != null)
        {
            if(_playerInput.JumpHeld)
            {
                useAbilityRequested = true;
            }
            if (_playerInput.JumpUp)
            {
                stopAbilityRequested = true;
            }
        }
        UpdateAbilityLineRenderer();
    }

    private void FixedUpdate()
    {
        if (CurrentNPC != null)
        {
            if (useAbilityRequested)
            {
                CurrentNPC.UseAbility();
                useAbilityRequested = false;

                if (!abilityActive)
                {
                    OnAbilityStart();
                    abilityActive = true;
                }

                if (_abilityCoroutine == null && CurrentNPC.CurrentNPCState == NPCState.InBag)
                {
                    Rigidbody rb = _connectedNPC.GetComponent<Rigidbody>();
                    Vector3 direction = camTransform.position - rb.position;
                    Quaternion lookRot = Quaternion.LookRotation(direction);
                    Quaternion newRot = Quaternion.Slerp(rb.rotation, lookRot, 10f * Time.fixedDeltaTime);

                    rb.MovePosition(npcAbilityPos.position);
                    rb.MoveRotation(newRot);
                }
            }

            if (stopAbilityRequested)
            {
                CurrentNPC.StopAbility();
                stopAbilityRequested = false;
                OnAbilityEnd();
                abilityActive = false;
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

    #region Ability Visuals

    private RigidbodyConstraints savedConstraints;
    private RigidbodyInterpolation savedInterpolation;
    private bool savedUseGravity;
    private bool savedUseKinematic;
    private int savedLayer;
    private Dictionary<Collider, bool> savedColliderStates = new Dictionary<Collider, bool>();

    public void OnAbilityStart()
    {
        if (CurrentNPC.CurrentNPCState != NPCState.InBag) return;

        if (_connectedNPC != null && _connectedNPC.TryGetComponent(out Rigidbody rb))
        {
            savedConstraints = rb.constraints;
            savedInterpolation = rb.interpolation;
            savedUseGravity = rb.useGravity;
            savedUseKinematic = rb.isKinematic;
            savedLayer = _connectedNPC.gameObject.layer;

            _connectedNPC.gameObject.layer = 7;
            foreach (Transform child in _connectedNPC)
                SetLayerRecursively(child.gameObject, 7);

            Collider[] colliders = _connectedNPC.GetComponents<Collider>();
            savedColliderStates.Clear();
            foreach (var col in colliders)
            {
                savedColliderStates[col] = col.isTrigger;
                col.isTrigger = true;
            }

            NPC_Pufferfish mama = CurrentNPC as NPC_Pufferfish;
            if (mama != null)
            {
                if (_abilityCoroutine != null)
                    StopCoroutine(_abilityCoroutine);

                Vector3 direction = camTransform.position - rb.position;
                Quaternion lookRot = Quaternion.LookRotation(direction);
                _abilityCoroutine = StartCoroutine(SmoothDampToPos(rb, npcAbilityPos, lookRot, mama.animDuration / 1.5f));
            }
        }
    }

    private IEnumerator SmoothDampToPos(Rigidbody rb, Transform target, Quaternion endRot, float duration)
    {
        if (_connectedNPC == null)
        {
            _abilityCoroutine = null;
            yield break;
        }

        yield return new WaitForSeconds(0.125f);

        if (stopAbilityRequested || !useAbilityRequested)
        {
            _abilityCoroutine = null;
            yield break;
        }

        Vector3 startPos = rb.position;
        Quaternion startRot = rb.rotation;
        float timer = 0f;

        _connectedNPC.SetParent(null);
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.None;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        NPC_Pufferfish mama = CurrentNPC as NPC_Pufferfish;
        StartCoroutine(mama.Inflate(mama.animDuration / 1.5f, 1.1f, false));

        while (timer < duration && _connectedNPC != null)
        {
            float t = timer / duration;
            t = Mathf.SmoothStep(0f, 1f, t);

            Vector3 newPos = Vector3.Lerp(startPos, target.position, t);
            Quaternion newRot = Quaternion.Slerp(startRot, endRot, t);

            rb.MovePosition(newPos);
            rb.MoveRotation(newRot);

            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        if (_connectedNPC != null)
        {
            _connectedNPC.SetParent(target);
            _connectedNPC.localPosition = Vector3.zero;

            rb.linearVelocity = Vector3.zero;
            abilityLineRenderer.enabled = true;
            abilityLineRenderer.SetPosition(0, npcBackpackPos.position);
            abilityLineRenderer.SetPosition(1, npcAbilityPos.position);
            _abilityCoroutine = null;
        }
    }

    public void OnAbilityEnd()
    {
        if (CurrentNPC.CurrentNPCState != NPCState.InBag) return;

        if (_connectedNPC != null && _connectedNPC.TryGetComponent(out Rigidbody rb))
        {
            InterruptAbility();

            NPC_Pufferfish mama = CurrentNPC as NPC_Pufferfish;
            if (mama != null)
            {
                StartCoroutine(mama.Deflate(mama.animDuration, mama.deflatedScale));

                if (_objectYankCoroutine != null)
                    StopCoroutine(_objectYankCoroutine);

                _objectYankCoroutine = StartCoroutine(YankObjectCoroutine(_connectedNPC, _connectedNPC, npcBackpackPos, true, false));
            }
        }
    }

    private void InterruptAbility()
    {
        if (_abilityCoroutine != null)
        {
            StopCoroutine(_abilityCoroutine);
            _abilityCoroutine = null;
        }

        abilityActive = false;
        useAbilityRequested = false;
        abilityLineRenderer.enabled = false;

        if (_connectedNPC != null && _connectedNPC.TryGetComponent(out Rigidbody rb))
        {
            _connectedNPC.SetParent(null);

            _connectedNPC.gameObject.layer = savedLayer;
            foreach (Transform child in _connectedNPC)
                SetLayerRecursively(child.gameObject, savedLayer);

            rb.constraints = savedConstraints;
            rb.useGravity = savedUseGravity;
            rb.isKinematic = savedUseKinematic;
            rb.interpolation = savedInterpolation;

            if (savedColliderStates != null)
            {
                foreach (var kvp in savedColliderStates)
                {
                    if (kvp.Key != null)
                    {
                        kvp.Key.isTrigger = kvp.Value;
                        kvp.Key.enabled = true;
                    }
                }
            }
        }
    }


    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private void UpdateAbilityLineRenderer()
    {
        if (CurrentNPC == null) return;

        if (CurrentNPC.CurrentNPCState != NPCState.InBag)
        {
            abilityLineRenderer.enabled = false;
            return;
        }

        if (useAbilityRequested)
        {
            abilityLineRenderer.SetPosition(0, npcBackpackPos.position);
            abilityLineRenderer.SetPosition(1, npcAbilityPos.position);
        }
        else
        {
            abilityLineRenderer.enabled = false;
        }
    }

    #endregion

    #region Object Yank

    public void HandleObjectYank()
    {
        if (CurrentNPC != null)
        {
            if (abilityActive || _abilityCoroutine != null)
            {
                InterruptAbility();
            }

            if (CurrentNPC.CurrentNPCState != NPCState.InBag)
            {
                _connectedNPC.GetComponent<Collider>().enabled = false;

                if (_objectYankCoroutine != null)
                    StopCoroutine(_objectYankCoroutine);

                _objectYankCoroutine = StartCoroutine(YankObjectCoroutine(_connectedNPC, _connectedNPC, npcBackpackPos, true, true));
                CurrentNPC.OnCaptureStart();
            }
            else
            {
                if (_objectYankCoroutine != null)
                    StopCoroutine(_objectYankCoroutine);

                _objectYankCoroutine = StartCoroutine(YankObjectCoroutine(_connectedNPC, _connectedNPC, npcRemovePos, false, true));
                CurrentNPC.OnReleaseStart();
            }
        }
    }

    private IEnumerator YankObjectCoroutine(Transform yankObj, Transform startPos, Transform endPos, bool attach, bool invokeEvent) //temp lol
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
            prop.OnHold(endPos);
            prop.AttachedTransform = transform;
            CurrentNPC.OnCaptureComplete();
            _playerLasso.SetToNoCollisionLayer(prop.gameObject);
        }
        else
        {
            prop.Rb.useGravity = false;
            prop.Rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            ReleaseNPC();
            CurrentNPC.OnReleaseComplete();
            _playerLasso.ResetLayer(prop.gameObject);
        }

        if (invokeEvent)
        {
            OnObjectYankCompleted?.Invoke();
            _objectYankCoroutine = null;
        }
    }

    #endregion

    #region NPC Handling

    private void SetConnectedNPC()
    {
        CurrentNPC = _playerLasso.SnaredObject as INPC;
        CurrentNPC.SetPlayerRef(transform);
        _connectedNPC = _playerLasso.SnaredObject.transform;
    }

    private void ReleaseNPC()
    {
        if (CurrentNPC == null || _objectYankCoroutine != null) return;

        CurrentNPC.SetPlayerRef(null);
        _connectedNPC = null;
        CurrentNPC = null;
    }

    #endregion
}
