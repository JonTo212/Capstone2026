using NodeCanvas.BehaviourTrees;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;
using static UnityEngine.Rendering.PostProcessing.HistogramMonitor;

[Serializable]
public struct NPCMultipliers
{
    public float heightMultiplier;
    public float cycleMultiplier;
    public float carryMultiplier;

    public NPCMultipliers(float height, float cycle, float carry)
    {
        heightMultiplier = height;
        cycleMultiplier = cycle;
        carryMultiplier = carry;
    }
}

public class NPC_Pufferfish : Prop, INPC
{
    [field: SerializeField] public NPCState CurrentNPCState { get; private set; }
    [field: SerializeField] public NPCType NPCType { get; private set; }
    public Transform NPCTransform { get; private set; }

    [Header("Deactivated")]
    [SerializeField] private float timeToCycle;
    [SerializeField] private float idleBobRange;
    [SerializeField] private float settleSpeed;

    [Header("Activated")]
    [SerializeField] private float freezeThreshold = 0.05f;
    [SerializeField] private float freezeDampSpeed = 2f;
    private bool pendingFreeze = false;

    [Header("InBag")]
    [SerializeField] private float playerSlowfallGravMultiplier;
    [SerializeField] private float playerWindForceMultiplier = 2f;
    [SerializeField] private float bagScale;
    private Transform playerTransform;

    [Header("Anim")]
    [field: SerializeField] public float inflatedScale { get; private set; }
    [field: SerializeField] public float deflatedScale { get; private set; }
    [field: SerializeField] public float animDuration { get; private set; }
    [SerializeField] private AnimationCurve animCurve;
    [SerializeField] private float defaultScale;
    private Coroutine animCoroutine;
    private Vector3 defaultLocalScale;

    #region Unity Functions
    private void Awake()
    {
        Init();
        Rb.useGravity = false;
        defaultLocalScale = Vector3.one;
        SwitchNPCState(NPCState.Deactivated);
        NPCTransform = transform;

        OnEnvironmentalForceSet += CheckIfInEnvironmentalForce;
    }

    private void OnDisable()
    {
        OnEnvironmentalForceSet -= CheckIfInEnvironmentalForce;
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        CounteractTetherForces();
        HandleNPCStateMachine();
        HandleFreezeDamping();
    }

    private void HandleNPCStateMachine()
    {
        switch (CurrentNPCState)
        {
            case NPCState.Deactivated:
                BobUpAndDown();
                break;

            case NPCState.Activated:
                DampenYVel();
                break;
        }

        DampenXZVel();
    }

    public void SwitchNPCState(NPCState newState)
    {
        if(CurrentNPCState == newState) return;
        CurrentNPCState = newState;
    }

    #endregion

    #region Activated/Deactivated 

    private void DampenXZVel()
    {
        float RbXVel = Mathf.Lerp(Rb.linearVelocity.x, 0f, Time.fixedDeltaTime * settleSpeed);
        float RbZVel = Mathf.Lerp(Rb.linearVelocity.z, 0f, Time.fixedDeltaTime * settleSpeed);

        Rb.linearVelocity = new Vector3(RbXVel, Rb.linearVelocity.y, RbZVel);
        Rb.angularVelocity *= 0.975f;
    }

    private void DampenYVel()
    {
        float RbYVel = Mathf.Lerp(Rb.linearVelocity.y, 0f, Time.fixedDeltaTime * settleSpeed * 5f);
        Rb.linearVelocity = new Vector3(Rb.linearVelocity.x, RbYVel, Rb.linearVelocity.z);
    }

    private void BobUpAndDown()
    {
        float bobSpeed = (Mathf.PI * 2f) / timeToCycle;
        float sinWaveVel = Mathf.Cos(Time.time * bobSpeed) * bobSpeed * idleBobRange;

        Rb.linearVelocity = new Vector3(Rb.linearVelocity.x, sinWaveVel, Rb.linearVelocity.z);
    }

    #endregion

    #region Helper Functions

    public void SetPlayerRef(Transform player)
    {
        playerTransform = player;
    }

    public void OnCaptureStart()
    {
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(Deflate(animDuration, deflatedScale));

        DestroyAllAttachedTethers();
        SwitchNPCState(NPCState.PlayerInteracting);
    }

    public void OnCaptureComplete()
    {
        SwitchNPCState(NPCState.InBag);
    }

    public void OnReleaseStart()
    {
        if(animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(Inflate(animDuration, inflatedScale, true));

        SwitchNPCState(NPCState.PlayerInteracting);
    }

    public void OnReleaseComplete()
    {
        SwitchNPCState(NPCState.Activated);
    }
    #endregion

    #region Overrides / Temp EnvironmentalForce Stuff

    public override void OnSnare(Lasso lassoRef)
    {
        base.OnSnare(lassoRef);
        Rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        pendingFreeze = false;
        SwitchNPCState(NPCState.Activated);
    }

    public override void OnTetherPull(JointTether tether, Transform targetAnchorTransform, Transform targetObjectTransform)
    {
        base.OnTetherPull(tether, targetAnchorTransform, targetObjectTransform);
        if (EnvironmentalForce == null)
        {
            pendingFreeze = true;
        }
        SwitchNPCState(NPCState.Activated);
    }

    public override void OnRelease()
    {
        base.OnRelease();
        if (attachedTethers.Count > 0)
        {
            if (EnvironmentalForce == null)
            {
                pendingFreeze = true;
                return;
            }
        }

        Rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        SwitchNPCState(NPCState.Deactivated);
    }

    public override void OnDetachTether(JointTether tether, Transform targetAnchorTransform, Transform targetObjectTransform)
    {
        base.OnDetachTether(tether, targetAnchorTransform, targetObjectTransform);
        if (attachedTethers.Count > 0)
        {
            if (EnvironmentalForce == null)
            {
                pendingFreeze = true; //this needs to change -> find a new way to stop tether forces
                return;
            }
        }

        Rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        SwitchNPCState(NPCState.Deactivated);
    }

    private void CounteractTetherForces()
    {
        Vector3 netJointForce = Vector3.zero;
        for (int i = 0; i < attachedTethers.Count; i++)
        {
            if (attachedTethers[i] == null) continue;
            if (connectedObject[i].TryGetComponent(out Prop prop) && prop.IsSnared) continue;

            netJointForce += attachedTethers[i].GetCurrentForce(Rb);
        }

        float mag = netJointForce.magnitude;

        if (mag > 0f)
        {
            Rb.AddForce(Vector3.down * netJointForce.y, ForceMode.Force);
        }
    }

    private void CheckIfInEnvironmentalForce()
    {
        if (CurrentNPCState == NPCState.InBag)
        {
            pendingFreeze = false;
            Rb.linearVelocity = Vector3.zero;
            Rb.angularVelocity = Vector3.zero;
            return;
        }

        if (EnvironmentalForce == null && attachedTethers.Count > 0 && !IsSnared)
        {
            pendingFreeze = true;
        }
        else
        {
            Rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    private void HandleFreezeDamping()
    {
        if (!pendingFreeze) return;

        Rb.linearVelocity = Vector3.MoveTowards(Rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * freezeDampSpeed);
        Rb.angularVelocity = Vector3.MoveTowards(Rb.angularVelocity, Vector3.zero, Time.fixedDeltaTime * freezeDampSpeed);

        if (Rb.linearVelocity.magnitude < freezeThreshold)
        {
            Rb.linearVelocity = Vector3.zero;
            Rb.angularVelocity = Vector3.zero;
            Rb.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            pendingFreeze = false;
        }
    }

    #endregion

    #region Ability

    public void UseAbility()
    {
        if (playerTransform == null || CurrentNPCState != NPCState.InBag) return;

        if (playerTransform.TryGetComponent(out PlayerMovement playerMovement))
        {
            if (EnvironmentalForce != null)
            {
                playerMovement.EnableGravity(false);
                Vector3 force = EnvironmentalForce.CalculateForce() * playerWindForceMultiplier;
                playerMovement.Rb.AddForce(force, ForceMode.Acceleration);
                Vector3 playerVel = playerMovement.Rb.linearVelocity;

                playerMovement.ApplyFriction(ref playerVel, Vector3.up); //needa do this to match sideways/vertical movement
            }
            else
            {
                playerMovement.EnableGravity(true);
                playerMovement.ApplySlowFall(playerSlowfallGravMultiplier);
            }
            AudioManager.Instance.TempPlayOneShot(AudioManager.Instance.BM_Glide, 8, 1f);
        }
    }

    public void StopAbility()
    {
        if(playerTransform.TryGetComponent(out PlayerMovement playerMovement))
        {
            playerMovement.EnableGravity(true);
            playerMovement.ResetGravity();
        }

        AudioManager.Instance.StopSFX(8);
    }

    #endregion

    #region TEMP - Anim

    public IEnumerator Inflate(float duration, float scale, bool enableCollider)
    {
        
        float elapsedTime = 0;
        Vector3 startScale = transform.localScale;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            float curveValue = animCurve.Evaluate(t);

            transform.localScale = Vector3.Lerp(startScale, defaultLocalScale * scale, curveValue);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localScale = defaultLocalScale * scale;
        if(enableCollider) GetComponent<Collider>().enabled = true;
        animCoroutine = null;
    }

    public IEnumerator Deflate(float duration, float scale)
    {
        float elapsedTime = 0;
        Vector3 startScale = transform.localScale;
        GetComponent<Collider>().enabled = false;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            float curveValue = animCurve.Evaluate(t);

            transform.localScale = Vector3.Lerp(startScale, defaultLocalScale * scale, curveValue);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localScale = defaultLocalScale * scale;
        animCoroutine = null;
    }

    #endregion

    protected override void CoyoteFall()
    {
        //do nothing, no gravity re-enable
    }
}
