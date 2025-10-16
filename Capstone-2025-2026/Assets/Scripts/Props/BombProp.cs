using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class BombProp : Prop, IActivatable
{
    [Header("Explosion Variables")]
    [SerializeField] private float fuseDuration;
    [SerializeField] private float explosionRadius;
    [SerializeField] private float explosionForce;
    [SerializeField] private float impactVelocityThreshold;

    //private runtime variables
    private Coroutine fuseCoroutine;
    private float fuseTimer;
    private bool impactExplode;

    //parameters
    private bool isActive;
    public bool IsActive => isActive;

    private void Awake()
    {
        base.Init();
    }

    #region IActivatable Functions (Activate/Deactivate)
    public void Activate()
    {
        if (fuseCoroutine != null)
        {
            impactExplode = true;
            return;
        }
        fuseCoroutine = StartCoroutine(HandleCountdown());
        isActive = true;
    }
    public void Deactivate()
    {
        fuseTimer = 0;
        isActive = false;
        if (fuseCoroutine != null)
        {
            StopCoroutine(fuseCoroutine);
        }
    }
    #endregion

    #region Pre-explosion Handling
    private IEnumerator HandleCountdown()
    {
        fuseTimer = 0;

        while (fuseTimer < fuseDuration)
        {
            fuseTimer += Time.deltaTime;
            if (impactExplode) break;

            yield return null;
        }

        Explode();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (Rb.linearVelocity.magnitude > impactVelocityThreshold && isActive)
        {
            impactExplode = true;
        }
    }

    #endregion

    #region Explosion function
    private void Explode()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider collider in hitColliders)
        {
            if(collider.TryGetComponent(out Prop prop))
            {
                Vector3 explosionPos = transform.position;
                Vector3 dir = prop.transform.position - explosionPos;
                float falloff = 1f - (dir.magnitude / explosionRadius);
                prop.ApplyForceInDirection(dir.normalized, explosionForce * falloff, ForceMode.Impulse);
            }
            else if(collider.TryGetComponent(out Rigidbody colRb))
            {
                colRb.AddExplosionForce(explosionForce, transform.position, explosionRadius, 2f, ForceMode.Impulse);
            }
        }

        OnRelease();
        Destroy(gameObject);
    }
    #endregion
}
