using System;
using System.Collections.Generic;
using UnityEngine;

public class PropSpawnPile : Prop
{
    [SerializeField] private Prop objectToSpawn;
    [SerializeField] private int poolSize = 3;
    [SerializeField] private Transform spawnDirection;

    [SerializeField] private Transform spawnLocation;
    [SerializeField] private GameObject DustEffect;

    private Queue<Prop> _pool;

    private void Awake()
    {
        base.Init();
        Rb.isKinematic = true;
        WarmPool();
    }

    private void Start()
    {
        if(spawnLocation != null)
        {
            DefaultSpawn();
        }
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

    private void SpawnPropOnPluck()
    {
        Prop stale = _pool.Dequeue();
        Instantiate(DustEffect, stale.AttachedTransform);
        stale.gameObject.SetActive(false);
        stale.DestroyAllAttachedTethers();

        if (stale.Rb != null)
        {
            stale.Rb.linearVelocity = Vector3.zero;
            stale.Rb.angularVelocity = Vector3.zero;
        }

        stale.transform.position = spawnDirection != null ? spawnDirection.position : transform.position;
        stale.transform.rotation = Quaternion.Euler(0f, spawnDirection != null ? spawnDirection.eulerAngles.y : 0f, 0f);
        stale.gameObject.SetActive(true);

        _playerRefData.Lasso.SetupHeldProp(stale, null);
        _pool.Enqueue(stale);
    }

    private void DefaultSpawn()
    {
        Prop stale = _pool.Dequeue();
        stale.gameObject.SetActive(false);

        if (stale.Rb != null)
        {
            stale.Rb.linearVelocity = Vector3.zero;
            stale.Rb.angularVelocity = Vector3.zero;
        }

        stale.transform.position = spawnLocation != null ? spawnLocation.position : transform.position;
        stale.transform.rotation = Quaternion.Euler(0f, spawnDirection != null ? spawnDirection.eulerAngles.y : 0f, 0f);
        stale.gameObject.SetActive(true);

        _pool.Enqueue(stale);
    }
}