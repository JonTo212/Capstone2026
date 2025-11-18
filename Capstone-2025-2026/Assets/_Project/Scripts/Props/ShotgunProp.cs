using UnityEngine;

public class ShotgunProp : Prop, IActivatable
{
    [SerializeField] private float maxAmmo;
    [SerializeField] private float shotRange;
    [SerializeField] private float shotForce;
    [SerializeField] private Transform barrel;

    private float currentAmmo;
    private bool isActive = false;
    public bool IsActive => isActive;

    private void Awake()
    {
        base.Init();
        currentAmmo = maxAmmo;
    }

    public override void OnHold(Transform newParent)
    {
        base.OnHold(newParent);
        Rb.isKinematic = true;

    }

    public override void OnThrow(Vector3 dir, float magnitude)
    {
        Rb.isKinematic = false;
        base.OnThrow(dir, magnitude);
    }

    public void Activate()
    {
        if (!isActive)
        {
            isActive = true;
            return;
        }

        if (AttachedTransform == null) return;

        if (currentAmmo > 1 && isActive)
        {
            CheckHits();
        }
        else
        {
            CheckHits();
            Deactivate();
        }
    }

    public void Deactivate()
    {
        isActive = false;
        AttachedTransform = null;
    }

    private void CheckHits()
    {
        Collider[] colliders = Physics.OverlapSphere(barrel.position, shotRange);
        foreach (var col in colliders)
        {
            if(col.TryGetComponent(out Rigidbody colRb))
            {
                Vector3 closestPoint = col.ClosestPoint(barrel.position);
                if ((closestPoint - barrel.position).sqrMagnitude < shotRange * shotRange)
                {
                    Vector3 toOther = colRb.position - barrel.position;
                    if (Vector3.Dot(barrel.forward, toOther) > 0.5f)
                    {
                        Vector3 closestPointOnTarget = col.ClosestPoint(barrel.position);
                        Vector3 rayDirection = closestPointOnTarget - barrel.position;

                        float distanceFalloff = 1f - Mathf.Clamp01(rayDirection.magnitude / shotRange);
                        colRb.AddForce(barrel.forward * distanceFalloff * shotForce, ForceMode.Impulse);

                        if (colRb.TryGetComponent(out IActivatable activatable))
                        {
                            activatable.Activate();
                        }
                    }
                }
            }
        }
        AttachedTransform.GetComponent<Rigidbody>().AddForce(-barrel.forward * shotForce, ForceMode.Impulse);
        currentAmmo--;
    }
}
