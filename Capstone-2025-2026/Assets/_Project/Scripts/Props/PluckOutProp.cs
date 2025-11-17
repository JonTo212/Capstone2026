using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PluckOutProp : Prop
{
    [Header("Pluck Prop Parameters")]
    [SerializeField] private bool pluckHorizontally = false;
    [SerializeField] private float pluckForceMin = 50f;
    [SerializeField] private float pluckDelay = 0.5f;
    [SerializeField] private float verticalForceMultiplier = 1f;
    [SerializeField] private float horizontalForceMultiplier = 1f;
    [SerializeField] private bool hasBeenPlucked = false;

    private Coroutine pluckCoroutine;

    private void Awake()
    {
        Init();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(!hasBeenPlucked)
        {
            Rb.isKinematic = true;
        }
    }

    // Update is called once per frame
    protected override void Update()
    {
        if(!hasBeenPlucked) TryPluckProp();
    }

    protected void TryPluckProp()
    {
        Vector3 localForceVector = transform.InverseTransformVector(totalForceApplied);

        float totalForceMagnitude;

        //pluck horizontally considers both horizontal and vertical force as valid. Each direction's multiplier is applied
        if (pluckHorizontally)
        {
            float xForce = localForceVector.x * horizontalForceMultiplier;
            float yForce = Mathf.Clamp(localForceVector.y * verticalForceMultiplier, 0, localForceVector.y);
            float zForce = localForceVector.z * horizontalForceMultiplier;

            totalForceMagnitude = new Vector3(zForce, xForce, yForce).magnitude;
        }
        else
        {
            totalForceMagnitude = Mathf.Clamp(localForceVector.y * verticalForceMultiplier, 0, localForceVector.y);
        }

        //plucks after a delay, force needs to be above the minimum for as long as the delay
        if (totalForceMagnitude > pluckForceMin)
        {
            if(pluckCoroutine == null)
            {
                pluckCoroutine = StartCoroutine(PluckAfterDelay(pluckDelay));
            }
        }
        else
        {
            pluckCoroutine = null;
        }
    }

    protected virtual void OnPluck()
    {
        Rb.isKinematic = false;
        hasBeenPlucked = true;
    }

    IEnumerator PluckAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        OnPluck();
        pluckCoroutine = null;
    }
}
