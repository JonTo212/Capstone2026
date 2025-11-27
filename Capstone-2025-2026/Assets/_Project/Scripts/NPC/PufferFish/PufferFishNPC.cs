using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PufferFishNPC : Prop, INPC
{
    [Serializable]
    private struct NPCMultipliers
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

    [Header("PufferFish Properties")]
    [Header("Idle Properties")]
    [SerializeField] private float timeToCycle;
    [SerializeField] private float idleBobRange;
    [SerializeField] private float inflateDuration;
    [SerializeField] private float deflateDuration;

    [Header("Ability Properties")]
    [SerializeField] private float carryStrength;
    [SerializeField] private float timeToMaxCarryStrength;
    [SerializeField] private float carryMaxHeight;
    [SerializeField] private float abilityDuration;
    private float abilityCooldownTimer;
    private bool abilityActive;
    private bool abilityOnCooldown;


    [Header("Disturbed Properties")]
    [SerializeField] private float uprightOrientationStrength;
    [SerializeField] private float returnToIdleDelay;
    private float returnToIdleTimer;


    [Header("States/Type")]
    [field: SerializeField] public NPCState CurrentNPCState { get; private set; } = NPCState.Idle;
    [field: SerializeField] public NPCType type { get; }

    private Transform playerTransform;

    [Header("TEMP - Inflation Anim")]
    [SerializeField] private AnimationCurve tempAnimCurve;
    [SerializeField] private float animDuration = 0.2f;
    [SerializeField] private float deflatedScaleMultiplier = 0.5f;
    private Coroutine animCoroutine;
    private Vector3 inflatedScale;
    private Vector3 deflatedScale;
    private bool inflated;
    private float inflationTimer;

    #region Unity Functions
    private void Awake()
    {
        Init();
        Rb.useGravity = false;

        inflatedScale = transform.localScale;
        deflatedScale = inflatedScale * deflatedScaleMultiplier;
        inflated = true;

        OnPropTethered += OnInteractedResponse;
        OnPropReleased += CheckOnRelease;
    }

    private void OnDisable()
    {
        OnPropTethered -= OnInteractedResponse;
        OnPropReleased -= CheckOnRelease;
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

    public override void OnSnare()
    {
        base.OnSnare();
        OnInteractedResponse();
    }

    #endregion

    #region Helper Functions
    public void SwitchNPCState(NPCState newState)
    {
        CurrentNPCState = newState;
    }

    private void OnInteractedResponse()
    {
        if (CurrentNPCState == NPCState.UsingAbility) return;

        SwitchNPCState(NPCState.Disturbed);
        Rb.linearVelocity = Vector3.zero;
    }

    public void OnEnteredPlayerBag()
    {
        SwitchNPCState(NPCState.Attached);
    }

    public void RemoveFromPlayerBag()
    {
        transform.parent = null;
        transform.rotation = playerTransform.GetComponent<PlayerNPCHolder>().npcRemovePos.rotation;
        transform.position = playerTransform.GetComponent<PlayerNPCHolder>().npcRemovePos.position;
        GetComponent<Collider>().enabled = true;

        Rb.constraints = RigidbodyConstraints.FreezeRotation;
        Rb.useGravity = false;
        
        if(animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(Inflate());

        playerTransform = null;
        SwitchNPCState(NPCState.Disturbed);
    }

    private void CheckOnRelease()
    {
        if (connectedObject.Count > 0)
        {
            if (CurrentNPCState == NPCState.Disturbed)
            {
                SwitchNPCState(NPCState.UsingAbility);
                StartRise();
            }
        }
        else
        {
            returnToIdleTimer = 0f;
            SwitchNPCState(NPCState.Disturbed);
        }
    }
    #endregion

    #region State Machine
    private void HandleNPCStateMachineUpdate()
    {
        switch (CurrentNPCState)
        {
            case NPCState.Idle:
                //InflateDeflate();
                break;

            case NPCState.UsingAbility:
                
                break;

            case NPCState.Disturbed:
                RunIdleCooldown();
                break;
        }
    }

    private void HandleNPCStateMachineFixedUpdate()
    {
        switch (CurrentNPCState)
        {
            case NPCState.Idle:
                BobUpAndDown();
                OrientUpwards();
                break;

            case NPCState.UsingAbility:
                UseAbility();
                break;
                
            case NPCState.Disturbed:
                //OrientUpwards();
                Rb.angularVelocity = Vector3.zero;
                break;
        }
    }
    #endregion

    #region Idle
    private void BobUpAndDown()
    {
        float bobSpeed = (Mathf.PI * 2f) / timeToCycle;
        float sin = Mathf.Sin(Time.time * bobSpeed) * Time.fixedDeltaTime;
        float sinWaveVel = Mathf.Cos(Time.time * bobSpeed) * bobSpeed * idleBobRange;

        float RbXVel = Mathf.Lerp(Rb.linearVelocity.x, 0f, Time.fixedDeltaTime * 2f);
        float RbZVel = Mathf.Lerp(Rb.linearVelocity.z, 0f, Time.fixedDeltaTime * 2f);
        Rb.linearVelocity = new Vector3(RbXVel, sinWaveVel, RbZVel);
    }

    private void InflateDeflate()
    {
        if (inflationTimer > 0f)
        {
            inflationTimer -= Time.deltaTime;
            return;
        }

        if (animCoroutine == null)
        {
            if (inflated)
                animCoroutine = StartCoroutine(Deflate(animDuration));
            else
                animCoroutine = StartCoroutine(Inflate());
        }
    }

    private void OrientUpwards()
    {
        Quaternion target = Quaternion.FromToRotation(transform.up, Vector3.up) * transform.rotation;
        Quaternion delta = target * Quaternion.Inverse(transform.rotation);

        delta.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f) angle -= 360f;
        angle = Mathf.Deg2Rad * angle;

        Vector3 orientationTorque = axis.normalized * angle * uprightOrientationStrength;
        float damping = 2f * Mathf.Sqrt(uprightOrientationStrength);
        Vector3 dampingTorque = -Rb.angularVelocity * damping;
        Vector3 totalTorque = orientationTorque + dampingTorque;

        Rb.AddTorque(totalTorque, ForceMode.Acceleration);
    }
    #endregion

    #region Ability

    private float velocityRef = 0f;
    private float targetPosY = 0f;
    private float startingPosY = 0f;
    private float smoothTime = 0f;

    private void StartRise()
    {
        if(animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(Inflate());

        startingPosY = Rb.position.y;
        targetPosY = startingPosY + carryMaxHeight;
        smoothTime = timeToMaxCarryStrength;
        velocityRef = 0f;
    }

    private void MoveUp()
    {
        float currentY = Rb.position.y;
        float newY = Mathf.SmoothDamp(currentY, targetPosY, ref velocityRef, smoothTime, Mathf.Infinity, Time.fixedDeltaTime);
        float velY = (newY - currentY) / Time.fixedDeltaTime;

        Rb.linearVelocity = new Vector3(0f, velY, 0f);

        if (Mathf.Abs(currentY - targetPosY) < 0.01f)
        {
            Rb.position = new Vector3(Rb.position.x, targetPosY, Rb.position.z);
            Rb.linearVelocity = Vector3.zero;
            SwitchNPCState(NPCState.Disturbed);
        }
    }

    public void AttachToPlayer(Transform player)
    {
        playerTransform = player;
    }   

    public void UseAbility()
    {
        if(playerTransform != null && playerTransform.TryGetComponent(out PlayerMovement playerMovement))
        {
            playerMovement.ApplySlowFall(0.33f);
        }
        else
        {
            MoveUp();
        }
    }
    private void HandleAbilityTimers()
    {
        if (abilityActive)
        {
            abilityCooldownTimer += Time.deltaTime;
            if (abilityCooldownTimer >= abilityDuration)
            {
                abilityActive = false;
                abilityOnCooldown = true;
                abilityCooldownTimer = 0f;

                SwitchNPCState(NPCState.Disturbed);
            }
        }

        if (abilityOnCooldown)
        {
            abilityCooldownTimer += Time.deltaTime;
            if (abilityCooldownTimer >= returnToIdleDelay)
            {
                abilityOnCooldown = false;
            }
        }
    }
    #endregion

    #region Disturbed

    private void RunIdleCooldown()
    {
        if(connectedObject.Count > 0) return;

        returnToIdleTimer += Time.deltaTime;
        if(returnToIdleTimer >= returnToIdleDelay)
        {
            SwitchNPCState(NPCState.Idle);
            returnToIdleTimer = 0f;
        }
    }    

    #endregion

    #region TEMP - Anim

    public void RunCaptureAnim()
    {
        if (animCoroutine != null)
            StopCoroutine(animCoroutine);

        animCoroutine = StartCoroutine(Deflate(animDuration));
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

    private IEnumerator Deflate(float duration)
    {
        float elapsedTime = 0;
        Vector3 startScale = transform.localScale;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            float curveValue = tempAnimCurve.Evaluate(t);

            transform.localScale = Vector3.Lerp(startScale, deflatedScale, curveValue);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localScale = deflatedScale;
        animCoroutine = null;
        inflationTimer = deflateDuration;
        inflated = false;
    }

    #endregion

    public void InflateBoost()
    {

    }

    protected override void CoyoteFall()
    {
        
    }
}
