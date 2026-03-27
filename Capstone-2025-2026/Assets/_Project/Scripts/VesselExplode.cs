using FMODUnity;
using UnityEngine;
using System.Collections;

public class VesselExplode : MonoBehaviour
{

    [SerializeField] MeshRenderer mr;
    [SerializeField] private Rigidbody rb;

    [SerializeField] private bool selfDestructionStarted = false;
    public GameObject explodeParticle;


    //Materials
    [SerializeField] Material origionalMaterial;
    [SerializeField] Material onDamageMat;
    [SerializeField] float hitFlashDuration;
    [SerializeField] private float explosionTimer;

    public float minimumBreakableVelocity;


    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        mr = GetComponent<MeshRenderer>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        Vector3 velocity = rb.linearVelocity;

        if (velocity.magnitude > minimumBreakableVelocity)
        {
            if (!selfDestructionStarted)
            {
                StartCoroutine(VesselSelfDestruct(explosionTimer));
                selfDestructionStarted=true;
            }

        }
    }

    private IEnumerator VesselSelfDestruct(float timeToExplode)
    {
        //Hitflash material
        mr.material = onDamageMat;

        yield return new WaitForSeconds(hitFlashDuration);

        mr.material = origionalMaterial;


        yield return new WaitForSeconds(timeToExplode);

        //play SFX
        RuntimeManager.PlayOneShot("event:/WallBreak", transform.position);
        Instantiate(explodeParticle, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
