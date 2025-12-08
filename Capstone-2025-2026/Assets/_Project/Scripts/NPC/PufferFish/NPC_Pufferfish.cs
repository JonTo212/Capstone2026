using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;

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
    [field: SerializeField] public NPCType Type { get; private set; }

    [Header("Deactivated")]
    [SerializeField] private float timeToCycle;
    [SerializeField] private float idleBobRange;
    [SerializeField] private float settleSpeed;

    [Header("InBag")]
    [SerializeField] private float playerSlowfallGravMultiplier;
    [SerializeField] private float bagScale;
    private Transform playerTransform;

    [Header("Anim")]
    [SerializeField] private AnimationCurve animCurve;
    [SerializeField] private float defaultScale;
    [SerializeField] private float inflatedScale;
    [SerializeField] private float deflatedScale;
    [SerializeField] private float animDuration;
    private Coroutine animCoroutine;
    private Vector3 defaultLocalScale;

    private void Awake()
    {
        Init();
        Rb.useGravity = false;
        defaultLocalScale = Vector3.one;
        SwitchNPCState(NPCState.Deactivated);

        OnPropTethered += OnTethersAttached;
        OnPropReleased += OnSnareTetherDetached;
        OnTetherDetached += OnSnareTetherDetached;
    }

    private void OnDisable()
    {
        OnPropTethered -= OnTethersAttached;
        OnPropReleased -= OnSnareTetherDetached;
        OnTetherDetached -= OnSnareTetherDetached;
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        CounteractTetherForces();
        HandleNPCStateMachine();

        //face player
        transform.LookAt(playerTransform.position);
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
        animCoroutine = StartCoroutine(Deflate(animDuration));

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
        animCoroutine = StartCoroutine(Inflate(animDuration));

        SwitchNPCState(NPCState.PlayerInteracting);
    }

    public void OnReleaseComplete()
    {
        SwitchNPCState(NPCState.Activated);
    }

    public override void OnSnare()
    {
        base.OnSnare();
        Rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        SwitchNPCState(NPCState.Activated);
    }

    private void OnTethersAttached()
    {
        //Rb.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        SwitchNPCState(NPCState.Activated);
    }

    private void OnSnareTetherDetached()
    {
        if (IsSnared) return;
        if (attachedTethers.Count > 0)
        {
            //Rb.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; //this needs to change -> find a new way to stop tether forces
            return;
        }

        Rb.constraints =  RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        SwitchNPCState(NPCState.Deactivated);
    }

    private void CounteractTetherForces()
    {
        Vector3 netJointForce = Vector3.zero;
        for (int i = 0; i < attachedTethers.Count; i++)
        {
            if (attachedTethers[i] == null) continue;
            netJointForce += attachedTethers[i].GetCurrentForce(Rb);
        }

        float mag = netJointForce.magnitude;

        if (mag > 0f)
        {
            Rb.AddForce(Vector3.down * netJointForce.y, ForceMode.Force);
        }
    }

    private void DestroyAllAttachedTethers()
    {
        List<JointTether> tetherCopies = new List<JointTether>(attachedTethers);
        foreach(var tether in tetherCopies)
        {
            tether.DestroyTether();
        }
    }

    #endregion

    #region Ability

    public void UseAbility()
    {
        if (playerTransform == null || CurrentNPCState != NPCState.InBag) return;

        if(playerTransform.TryGetComponent(out PlayerMovement playerMovement))
        {
            if (EnvironmentalForce != null)
            {
                playerMovement.OverrideMovement(EnvironmentalForce.CalculateForce(playerMovement.Rb));
                playerMovement.EnableGravity(false);
            }
            else
            {
                playerMovement.OverrideMovement(Vector3.zero);
                playerMovement.ApplySlowFall(playerSlowfallGravMultiplier);
            }
        }
    }

    public void StopAbility()
    {
        if(playerTransform.TryGetComponent(out PlayerMovement playerMovement))
        {
            playerMovement.OverrideMovement(Vector3.zero);
            playerMovement.EnableGravity(true);
            playerMovement.ResetGravity();
        }
    }

    #endregion

    #region TEMP - Anim

    private IEnumerator Inflate(float duration)
    {
        float elapsedTime = 0;
        Vector3 startScale = transform.localScale;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            float curveValue = animCurve.Evaluate(t);

            transform.localScale = Vector3.Lerp(startScale, defaultLocalScale * inflatedScale, curveValue);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localScale = defaultLocalScale * inflatedScale;
        GetComponent<Collider>().enabled = true;
        animCoroutine = null;
    }

    private IEnumerator Deflate(float duration)
    {
        float elapsedTime = 0;
        Vector3 startScale = transform.localScale;
        GetComponent<Collider>().enabled = false;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            float curveValue = animCurve.Evaluate(t);

            transform.localScale = Vector3.Lerp(startScale, defaultLocalScale * deflatedScale, curveValue);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localScale = defaultLocalScale * deflatedScale;
        animCoroutine = null;
    }

    #endregion

    protected override void CoyoteFall()
    {
        //do nothing, no gravity re-enable
    }
}
