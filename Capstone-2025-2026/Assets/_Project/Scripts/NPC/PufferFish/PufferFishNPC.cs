using System;
using System.Collections;
using UnityEngine;

public class PufferFishNPC : Prop, INPC
{
    [Header("Pufferfish Properties")]
    [SerializeField] private float idleFloatSpeed;
    [SerializeField] private float idleFloatRange;
    [SerializeField] private float floatStrength;
    [SerializeField] private float inflateDuration;
    [SerializeField] private float deflateDuration;
    [SerializeField] private float returnToIdleSpeed;

    [Header("States/Type")]
    public NPCState CurrentNPCState { get; private set; } = NPCState.Idle;
    [field: SerializeField] public NPCType type { get; }

    private Transform attachedObject;

    [Header("TEMP - Inflation Anim")]
    [SerializeField] private AnimationCurve tempAnimCurve;
    [SerializeField] private float animDuration = 0.2f;
    [SerializeField] private float deflatedScaleMultiplier = 0.2f;
    private Coroutine animCoroutine;
    private Vector3 inflatedScale;
    private Vector3 deflatedScale;
    private bool inflated;
    private float inflationTimer;

    public event Action OnAnimComplete;

    private void Awake()
    {
        Init();
        Rb.useGravity = false;

        inflatedScale = transform.localScale;
        deflatedScale = inflatedScale * deflatedScaleMultiplier;
        inflated = true;
    }

    protected override void Update()
    {
        base.Update();
        HandleNPCStateMachineUpdate();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        HandleNPCStateMachineFixedUpdate();
    }

    public void SwitchNPCState(NPCState newState)
    {
        CurrentNPCState = newState;
    }

    private void HandleNPCStateMachineUpdate()
    {
        switch (CurrentNPCState)
        {
            case NPCState.Idle:
                HandleInflation();
                break;

            case NPCState.UsingAbility:

                break;

            case NPCState.Attached:

                break;

            case NPCState.Disturbed:

                break;
        }
    }

    private void HandleNPCStateMachineFixedUpdate()
    {
        switch (CurrentNPCState)
        {
            case NPCState.Idle:
                BobUpAndDown();
                break;

            case NPCState.UsingAbility:
                UseAbility();
                break;

            case NPCState.Attached:

                break;
                
            case NPCState.Disturbed:
                
                break;
        }
    }

    #region Idle
    private void BobUpAndDown()
    {
        float sin = Mathf.Sin(Time.time * idleFloatSpeed) * Time.fixedDeltaTime;
        float sinWaveVel = Mathf.Cos(Time.time * idleFloatSpeed) * idleFloatSpeed * idleFloatRange;

        float RbXVel = Mathf.Lerp(Rb.linearVelocity.x, 0f, Time.fixedDeltaTime * returnToIdleSpeed);
        float RbZVel = Mathf.Lerp(Rb.linearVelocity.z, 0f, Time.fixedDeltaTime * returnToIdleSpeed);
        Rb.linearVelocity = new Vector3(RbXVel, sinWaveVel, RbZVel);
    }

    private void HandleInflation()
    {
        if (inflationTimer > 0f)
        {
            inflationTimer -= Time.deltaTime;
            return;
        }

        if (animCoroutine == null)
        {
            if (inflated)
                animCoroutine = StartCoroutine(Deflate());
            else
                animCoroutine = StartCoroutine(Inflate());
        }
    }

    #endregion

    #region Ability
    private void PullAttachedObjects()
    {
        foreach(var t in connectedObject)
        {
            if(t.TryGetComponent(out Rigidbody connectedRb))
            {
                connectedRb.AddForce(Vector3.up * floatStrength, ForceMode.Force);
            }
        }
    }

    public void AttachObject(Transform objTransform)
    {
        attachedObject = objTransform;
    }

    public void UseAbility()
    {
        if(attachedObject == null) return;

        if(attachedObject.TryGetComponent(out PlayerMovement playerMovement))
        {
            playerMovement.ApplySlowFall(0.33f);
        }
        else
        {
            PullAttachedObjects();
        }
    }
    #endregion


    #region TEMP - Anim

    public void RunCaptureAnim()
    {
        if (animCoroutine != null)
            StopCoroutine(animCoroutine);

        animCoroutine = StartCoroutine(Deflate());
    }


    private IEnumerator Inflate()
    {
        float elapsedTime = 0;
        Vector3 startScale = transform.localScale;

        while (elapsedTime < animDuration)
        {
            float t = elapsedTime / animDuration;
            float curveValue = tempAnimCurve.Evaluate(t);

            transform.localScale = Vector3.Lerp(startScale, inflatedScale, curveValue);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localScale = inflatedScale;
        inflationTimer = inflateDuration;
        inflated = true;
        animCoroutine = null;
    }

    private IEnumerator Deflate()
    {
        float elapsedTime = 0;
        Vector3 startScale = transform.localScale;

        while (elapsedTime < animDuration)
        {
            float t = elapsedTime / animDuration;
            float curveValue = tempAnimCurve.Evaluate(t);

            transform.localScale = Vector3.Lerp(startScale, deflatedScale, curveValue);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localScale = deflatedScale;
        animCoroutine = null;
        inflationTimer = deflateDuration;
        inflated = false;
        OnAnimComplete?.Invoke();
    }

    #endregion

    public void InflateBoost()
    {

    }

    protected override void CoyoteFall()
    {
        
    }
}
