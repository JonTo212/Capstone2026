using UnityEngine;

public class PufferFishNPC : NPCBase
{
    [Header("Pufferfish Properties")]
    [SerializeField] private float idleFloatSpeed;
    [SerializeField] private float idleFloatRange;
    [SerializeField] private float floatStrength;

    protected override void Awake()
    {
        base.Awake();
        Rb.useGravity = false;
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        if (!IsTetherPulled && !IsHeld && !IsSnared)
        {
            Float();
        }
        else
        {
            PullAttachedObject();
        }
    }

    private void Float()
    {
        float sin = Mathf.Sin(Time.time * idleFloatSpeed) * Time.deltaTime;
        float sinWaveVel = Mathf.Cos(Time.time * idleFloatSpeed) * idleFloatSpeed * idleFloatRange;
        Rb.linearVelocity = new Vector3(0f, sinWaveVel, 0f);
    }

    private void PullAttachedObject()
    {
        if(lassoRef != null)
        {
            lassoRef.PlayerController.Rb.AddForce(Vector3.up * floatStrength, ForceMode.Force);
        }
        if (connectedObject.Count > 0)
        {
            foreach(var t in connectedObject)
            {
                t.GetComponent<Rigidbody>().AddForce(Vector3.up * floatStrength, ForceMode.Force);
            }
        }

        Rb.AddForce(Vector3.up * floatStrength, ForceMode.Force);
    }
}
