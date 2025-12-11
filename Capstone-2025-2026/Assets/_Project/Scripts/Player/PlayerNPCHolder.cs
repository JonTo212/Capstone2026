using System;
using System.Collections;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class PlayerNPCHolder : MonoBehaviour
{
    public INPC CurrentNPC { get; private set; }
    [field: SerializeField] public Transform npcRemovePos { get; private set; }
    [field: SerializeField] public Transform npcBackpackPos { get; private set; }
    [field: SerializeField] public Transform npcAbilityPos { get; private set; }

    private PlayerActions _playerInput;
    private Lasso _playerLasso;

    [Header("Ability Handling")]
    [SerializeField] private LineRenderer abilityLineRenderer;
    private Transform _connectedNPC;
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
                else
                {
                    MoveToAbilitySpot();
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

    private RigidbodyConstraints savedConstraints;
    private bool savedUseGravity;
    private bool savedUseKinematic;
    private int savedLayer;

    #region Object Yank

    public void OnAbilityStart()
    {
        if (CurrentNPC.CurrentNPCState != NPCState.InBag) return;

        if (_connectedNPC != null && _connectedNPC.TryGetComponent(out Rigidbody rb))
        {
            savedConstraints = rb.constraints;
            savedUseGravity = rb.useGravity;
            savedUseKinematic = rb.isKinematic;
            savedLayer = _connectedNPC.gameObject.layer;

            _connectedNPC.gameObject.layer = 7;
            foreach (Transform child in _connectedNPC)
                SetLayerRecursively(child.gameObject, 7);

            Collider[] colliders = _connectedNPC.GetComponents<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }

            _connectedNPC.SetParent(npcAbilityPos);
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.None;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            NPC_Pufferfish mama = CurrentNPC as NPC_Pufferfish;
            if (mama != null)
            {
                StartCoroutine(mama.Inflate(mama.animDuration, 1.1f, false));
           }
        }
    }

    public void OnAbilityEnd()
    {
        if (CurrentNPC.CurrentNPCState != NPCState.InBag) return;

        if (_connectedNPC != null && _connectedNPC.TryGetComponent(out Rigidbody rb))
        {
            _connectedNPC.SetParent(null);

            rb.constraints = savedConstraints;
            rb.useGravity = savedUseGravity;
            rb.isKinematic = savedUseKinematic;
            _connectedNPC.gameObject.layer = savedLayer;

            foreach (Transform child in _connectedNPC)
                SetLayerRecursively(child.gameObject, savedLayer);

            NPC_Pufferfish mama = CurrentNPC as NPC_Pufferfish;
            if (mama != null)
            {
                StartCoroutine(mama.Deflate(mama.animDuration, mama.deflatedScale));

                if (_objectYankCoroutine != null)
                    StopCoroutine(_objectYankCoroutine);

                _objectYankCoroutine = StartCoroutine(YankObjectCoroutine(_connectedNPC, _connectedNPC, npcBackpackPos, true));
            }
        }
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    public void MoveToAbilitySpot()
    {
        if (CurrentNPC.CurrentNPCState != NPCState.InBag) return;

        if (_connectedNPC != null && _connectedNPC.TryGetComponent(out Rigidbody rb))
        {
            _connectedNPC.SetParent(npcAbilityPos);
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.None;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            _connectedNPC.localPosition = Vector3.zero;
            _connectedNPC.localRotation = Quaternion.identity;
            abilityLineRenderer.enabled = true;
            abilityLineRenderer.SetPosition(0, npcBackpackPos.position);
            abilityLineRenderer.SetPosition(1, npcAbilityPos.position);
        }
    }

    private void UpdateAbilityLineRenderer()
    {
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

    public void HandleObjectYank()
    {
        if (CurrentNPC != null)
        {
            if (CurrentNPC.CurrentNPCState != NPCState.InBag)
            {
                _connectedNPC.GetComponent<Collider>().enabled = false;

                if (_objectYankCoroutine != null)
                    StopCoroutine(_objectYankCoroutine);

                _objectYankCoroutine = StartCoroutine(YankObjectCoroutine(_connectedNPC, _connectedNPC, npcBackpackPos, true));
                CurrentNPC.OnCaptureStart();
            }
            else
            {
                if (_objectYankCoroutine != null)
                    StopCoroutine(_objectYankCoroutine);

                _objectYankCoroutine = StartCoroutine(YankObjectCoroutine(_connectedNPC, _connectedNPC, npcRemovePos, false));
                CurrentNPC.OnReleaseStart();
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
            prop.OnHold(endPos);
            prop.AttachedTransform = transform;
            CurrentNPC.OnCaptureComplete();
        }
        else
        {
            prop.Rb.useGravity = false;
            prop.Rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            ReleaseNPC();
            CurrentNPC.OnReleaseComplete();
        }

        OnObjectYankCompleted?.Invoke();
        _objectYankCoroutine = null;
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
