using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PluckOutProp : Prop
{
    [Header("Pluck Prop Parameters")]
    [SerializeField] private bool pluckHorizontally = false;
    [SerializeField] private float pluckForceMin = 50f;
    [SerializeField] private float pluckDelay = 0.5f;
    [SerializeField] private float verticalForceMultiplier = 1f;
    [SerializeField] private float horizontalForceMultiplier = 1f;
    [SerializeField] private bool hasBeenPlucked = false;
    [SerializeField] private float startDist = 0f;
    [SerializeField] private Lasso lassoRef;
    [SerializeField] private GameObject groundedVisuals;
    private float walkBackDist = 10f;
    private float playerStartDist = 0f;

    private Coroutine pluckCoroutine;

    private void Awake()
    {
        Init();
        if (!hasBeenPlucked)
        {
            Rb.isKinematic = true;
        }
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
        if(!hasBeenPlucked) TryPluckProp();
    }

    protected void TryPluckProp()
    {
        
        Vector3 localForceVector = transform.InverseTransformVector(totalForceApplied);

        float totalForceMagnitude = 0;
        bool stepCheck = false; 
        bool reelCheck = false;
        /*
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

        */

        //plucks after a delay, force needs to be above the minimum for as long as the delay
        if (lassoRef != null)
        {
            stepCheck = Vector3.Distance(transform.position, lassoRef.transform.position) > playerStartDist + walkBackDist;
            reelCheck = lassoRef.AnchorDist < startDist;
        }

        if (stepCheck || reelCheck) //Condition for code
        {
            if (pluckCoroutine == null)
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
        groundedVisuals.SetActive(false);
        lassoRef.HandleObjectReleased();
    }

    IEnumerator PluckAfterDelay(float delay)
    {
        //PlayerActions.Instance.DisableAllInput();
        PlayerActions.Instance.ChangeSpecificInput("Lasso", true);
        yield return new WaitForSeconds(delay);

        OnPluck();
        PlayerActions.Instance.EnableAllInput();
        pluckCoroutine = null;
    }

    public void SaveDist(float anchorStartDist, float playerStartDist, Lasso LassoFake)
    {
        lassoRef = LassoFake;
        startDist = anchorStartDist;
        playerStartDist = this.playerStartDist;
    }

    public void Reset()
    {
        lassoRef = null;
        startDist = 0;
        playerStartDist = 0;
    }
}
