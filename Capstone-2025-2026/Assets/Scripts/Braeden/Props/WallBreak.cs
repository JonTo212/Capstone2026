using UnityEngine;

public class WallBreak : MonoBehaviour
{
    public GameObject explodeParticle;
    public float minimumBreakableVelocity;


    private void OnCollisionEnter(Collision collision)
    {
        GameObject impactObject = collision.gameObject;

        if (impactObject.TryGetComponent(out DestructionCheck destructioncheckScript))
        {
            if (destructioncheckScript.canDestroy)
            {
                Break();
            }
        }

        else if (impactObject.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            Vector3 velocity = rb.linearVelocity;

            if (velocity.magnitude > minimumBreakableVelocity) { Break(); }

        }
    }

    private void Break()
    {
        Destroy(gameObject);

        //play particle effect
        Instantiate(explodeParticle, transform.position, Quaternion.identity);
    }

}
