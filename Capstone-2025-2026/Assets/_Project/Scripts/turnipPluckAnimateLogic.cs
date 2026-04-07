using FMODUnity;
using UnityEngine;

public class turnipPluckAnimateLogic : MonoBehaviour
{
    [Header("Break Parameters")]
    [SerializeField] private GameObject propToSpawnAfterBreak = null;
    [SerializeField] private Vector3 propSpawnOffset;


    [SerializeField] private int numberOfObjectsToSpawn = 1;
    [SerializeField] private float minimumSpeedToBreak = 5f;
    [SerializeField] private Vector2 spawnImpulseAmount = new Vector2(2f, 1f);
    //[SerializeField] private bool breakOnPluck = false;


    private Vector3 GetRandomSpawnForce()
    {
        Vector2 horizontalForce = Random.insideUnitCircle * spawnImpulseAmount.x;

        return new Vector3(horizontalForce.x, spawnImpulseAmount.y, horizontalForce.y);
    }

    private void DestroyObject()
    {
        RuntimeManager.PlayOneShot("event:/FruitBreak 2", transform.position);

        if (propToSpawnAfterBreak != null)
        {
            for (int i = 0; i < numberOfObjectsToSpawn; i++)
            {
                GameObject spawnedObject = Instantiate(propToSpawnAfterBreak, transform.position + propSpawnOffset, Quaternion.Euler(0, 0, 0));
                if (spawnedObject.TryGetComponent<Rigidbody>(out Rigidbody objectRb))
                {
                    objectRb.AddForce(GetRandomSpawnForce(), ForceMode.VelocityChange);
                }
            }
        }

        Destroy(gameObject);
    }




}
