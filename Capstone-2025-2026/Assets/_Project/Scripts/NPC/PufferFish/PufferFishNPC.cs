using System.Collections;
using UnityEngine;

public class PufferFishNPC : NPCBase
{
    [Header("Pufferfish Properties")]
    [SerializeField] private float idleFloatSpeed;
    [SerializeField] private float idleFloatRange;
    [SerializeField] private float floatStrength;
    [SerializeField] private float inflateCooldown;

    [Header("TEMP - Anim")]
    [SerializeField] private AnimationCurve tempAnimCurve;
    [SerializeField] private float animDuration = 0.2f;
    [SerializeField] private float inflationMultiplier = 0.2f;
    private Coroutine animCoroutine;
    [SerializeField] private PlayerActions tempInputRef;
    private Vector3 inflatedScale;
    private Vector3 deflatedScale;

    protected override void Awake()
    {
        base.Awake();
        Rb.useGravity = false;

        inflatedScale = transform.localScale;
        deflatedScale = inflatedScale * inflationMultiplier;
    }

    protected override void Update()
    {
        if(tempInputRef.SprintDown)
        {
            if (transform.localScale == inflatedScale)
            {
                animCoroutine = StartCoroutine(Deflate());
            }
            else if (transform.localScale == deflatedScale)
            {
                animCoroutine = StartCoroutine(Inflate());
            }
        }
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        if (!IsTetherPulled && !IsHeld && !IsSnared)
        {
            BobUpAndDown();
        }
        else
        {
            PullAttachedObject();
        }
    }

    private void BobUpAndDown()
    {
        float sin = Mathf.Sin(Time.time * idleFloatSpeed) * Time.deltaTime;
        float sinWaveVel = Mathf.Cos(Time.time * idleFloatSpeed) * idleFloatSpeed * idleFloatRange;
        Rb.linearVelocity = new Vector3(0f, sinWaveVel, 0f);
    }

    private void PullAttachedObject()
    {
        if(lassoRef != null)
        {
            PullIntoBag(lassoRef.transform);
        }
        if (connectedObject.Count > 0)
        {
            foreach(var t in connectedObject)
            {
                t.GetComponent<Rigidbody>().AddForce(Vector3.up * floatStrength, ForceMode.Acceleration);
            }
        }

        Rb.AddForce(Vector3.up * floatStrength, ForceMode.Acceleration);
    }

    public override void UseAbility()
    {
        PlayerMovement playerController = playerRef.GetComponent<PlayerMovement>();

        if (playerController != null)
        {
            float halfGrav = playerController.Gravity / 1.5f; 
            playerController.Rb.AddForce(Vector3.up * halfGrav, ForceMode.Acceleration);
        }
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
        animCoroutine = null;
    }

    public override void RunAnim()
    {
        if(animCoroutine != null)
            StopCoroutine(animCoroutine);

        animCoroutine = StartCoroutine(Deflate());
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
        RunAnimEvent();
    }

    public void InflateBoost()
    {

    }

    protected override void CoyoteFall()
    {
        
    }
}
