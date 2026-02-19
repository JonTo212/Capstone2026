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

    private float breakableDelay = 0.3f;
    private bool isBreakable = false;
    
    private Rigidbody rb;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Start()
    {
        base.Start();
        rb = GetComponent<Rigidbody>();
    }

    protected override void OnPluck()
    {

        base.OnPluck();
        RuntimeManager.PlayOneShot("event:/Pluck", transform.position);

        if (breakOnPluck) Break();

        StartCoroutine(BecomeBreakableDelay());

    }

    private void OnCollisionEnter(Collision collision)
    {
        if(isBreakable && rb.linearVelocity.magnitude > minimumSpeedToBreak)
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
        RuntimeManager.PlayOneShot("event:/RockBreak", transform.position);
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

        Destroy(gameObject);
    }    

    private IEnumerator BecomeBreakableDelay()
    {
        yield return new WaitForSeconds(breakableDelay);

        isBreakable = true;
    }
}
