using UnityEngine;

public class WallBreak : MonoBehaviour
{
    public GameObject explodeParticle;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent(out DestructionCheck destructioncheckScript))
        {
            if (destructioncheckScript.canDestroy)
            {
                Break();
            }
        }
    }

    private void Break()
    {
        Destroy(gameObject);

        //play particle effect
        Instantiate(explodeParticle, transform.position, Quaternion.identity);
    }

}
