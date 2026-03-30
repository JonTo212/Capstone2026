using System;
using System.Collections.Generic;
using UnityEngine;

public class PropSpawnPile : Prop
{
    [SerializeField] private Prop objectToSpawn;
    [SerializeField] private int poolSize = 3;
    [SerializeField] private Transform spawnDirection;

    private Queue<Prop> _pool;

    private void Awake()
    {
        base.Init();
        Rb.isKinematic = true;
        WarmPool();
    }

    private void WarmPool()
    {
        _pool = new Queue<Prop>(poolSize);
        for (int i = 0; i < poolSize; i++)
        {
            Prop instance = Instantiate(objectToSpawn);
            instance.gameObject.SetActive(false);
            _pool.Enqueue(instance);
        }
    }

    public override void OnSnare(PlayerRefData playerData)
    {
        base.OnSnare(playerData);
        SpawnPropOnPluck();
    }

    // PropSpawnPile.cs

    private void SpawnPropOnPluck()
    {
        Prop stale = _pool.Dequeue();
        stale.gameObject.SetActive(false);

        if (stale.Rb != null)
        {
            stale.Rb.linearVelocity = Vector3.zero;
            stale.Rb.angularVelocity = Vector3.zero;
        }

        stale.transform.position = spawnDirection != null ? spawnDirection.position : transform.position;
        stale.transform.rotation = Quaternion.Euler(0f, spawnDirection != null ? spawnDirection.eulerAngles.y : 0f, 0f);
        stale.gameObject.SetActive(true);

        _playerRefData.Lasso.SetupHeldProp(stale, GetFaceHit(stale));  // <-- pass computed hit

        _pool.Enqueue(stale);
    }

    private RaycastHit? GetFaceHit(Prop prop)
    {
        if (spawnDirection == null) return null;

        Collider col = prop.GetComponentInChildren<Collider>();
        if (col == null) return null;

        // Find the point on the prop's surface closest to the spawn origin
        Vector3 surfacePoint = col.ClosestPoint(spawnDirection.position);

        RaycastHit hit = new RaycastHit();

        // Raycast from just outside the surface back toward the prop center
        // to properly populate hit.point and hit.normal
        Vector3 direction = (prop.transform.position - spawnDirection.position).normalized;
        Vector3 rayOrigin = surfacePoint - direction * 0.01f;

        if (Physics.Raycast(rayOrigin, direction, out RaycastHit result, 0.1f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            hit = result;
        }
        else
        {
            // Fallback: manually fill in the point so CheckNearestGrabPoint still works
            hit.point = surfacePoint; // Unity doesn't expose a public setter for normal, but point is enough
        }

        return hit;
    }
}