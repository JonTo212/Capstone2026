using System;
using System.Collections;
using UnityEngine;

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
    public NPCState CurrentNPCState { get; private set; }
    public NPCType Type { get; private set; }


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
        SwitchNPCState(NPCState.Activated);
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        HandleNPCStateMachine();
    }

    private void HandleNPCStateMachine()
    {
        switch (CurrentNPCState)
        {
            case NPCState.Activated:
                BobUpAndDown();
                break;
        }
    }

    public void SwitchNPCState(NPCState newState)
    {
        CurrentNPCState = newState;
    }

    private void BobUpAndDown()
    {
        float bobSpeed = (Mathf.PI * 2f) / timeToCycle;
        float sinWaveVel = Mathf.Cos(Time.time * bobSpeed) * bobSpeed * idleBobRange;

        float RbXVel = Mathf.Lerp(Rb.linearVelocity.x, 0f, Time.fixedDeltaTime * settleSpeed);
        float RbZVel = Mathf.Lerp(Rb.linearVelocity.z, 0f, Time.fixedDeltaTime * settleSpeed);

        Rb.linearVelocity = new Vector3(RbXVel, sinWaveVel, RbZVel);
        Rb.angularVelocity *= 0.975f;
    }

    public void SetPlayerRef(Transform player)
    {
        playerTransform = player;
    }

    public void OnCaptureStart()
    {
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(Deflate(animDuration));
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


    #region Ability

    public void UseAbility()
    {
        if (playerTransform == null || CurrentNPCState != NPCState.InBag) return;

        if(playerTransform.TryGetComponent(out PlayerMovement playerMovement))
        {
            playerMovement.ApplySlowFall(playerSlowfallGravMultiplier);
        }
    }

    public void StopAbility()
    {
        if(playerTransform.TryGetComponent(out PlayerMovement playerMovement))
        {
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
