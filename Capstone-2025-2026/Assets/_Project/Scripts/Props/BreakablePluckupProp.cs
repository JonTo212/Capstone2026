using FMODUnity;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class BreakablePluckupProp : PluckOutProp
{
    [Header("Break Parameters")]
    [SerializeField] private GameObject propToSpawnAfterBreak = null;
    [SerializeField] private int numberOfObjectsToSpawn = 1;
    [SerializeField] private float minimumSpeedToBreak = 5f;
    [SerializeField] private Vector2 spawnImpulseAmount = new Vector2(2f,1f);
    [SerializeField] private bool breakOnPluck = false;

    public EndSequeenceTracker endSequeenceTracker;


    public Event OnPlucked;

    private float breakableDelay = 0.3f;
    private bool isBreakable = false;

    [SerializeField] private bool dontPlayDestroySound = false; // used for the specific turnip object that plays the sound after its animation in a different script

    protected override void OnPluck()
    {

        base.OnPluck();
        RuntimeManager.PlayOneShot("event:/Pluck", transform.position);

        if (breakOnPluck) Break();

        StartCoroutine(BecomeBreakableDelay());
        if(endSequeenceTracker != null)
        {
            endSequeenceTracker.UpdateSupports(0);
        }
    }

    public void OnPluckPublic() => OnPluck();

    private void OnCollisionEnter(Collision collision)
    {
        if (isBreakable && Rb.linearVelocity.magnitude > minimumSpeedToBreak)
        {
            Break();

        }
    }

    private Vector3 GetRandomSpawnForce()
    {
        Vector2 horizontalForce = Random.insideUnitCircle * spawnImpulseAmount.x;

        return new Vector3(horizontalForce.x, spawnImpulseAmount.y, horizontalForce.y);
    }

    private void Break()
    {
        if (!dontPlayDestroySound) RuntimeManager.PlayOneShot("event:/FruitBreak", transform.position);

        if (propToSpawnAfterBreak != null)
        {
            for (int i = 0; i < numberOfObjectsToSpawn; i++)
            {
                GameObject spawnedObject = Instantiate(propToSpawnAfterBreak, transform.position, Quaternion.Euler(0, 0, 0));
                if (spawnedObject.TryGetComponent<Rigidbody>(out Rigidbody objectRb))
                {
                    objectRb.AddForce(GetRandomSpawnForce(), ForceMode.VelocityChange);
                }
            }
        }


        gameObject.SetActive(false);
    }    

    private IEnumerator BecomeBreakableDelay()
    {
        yield return new WaitForSeconds(breakableDelay);

        isBreakable = true;
    }


}
