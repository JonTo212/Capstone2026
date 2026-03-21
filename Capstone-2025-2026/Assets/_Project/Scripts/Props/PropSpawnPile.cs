using System;
using UnityEngine;

public class PropSpawnPile : Prop
{
    private Prop previouslySpawnedProp;
    [SerializeField] private Prop objectToSpawn;
    
    public event Action OnPropSpawned;

    private void Awake()
    {
        base.Init();
        Rb.isKinematic = true;
    }

    public override void OnSnare(PlayerRefData playerData)
    {
        base.OnSnare(playerData);
        SpawnPropOnPluck();
    }

    private void SpawnPropOnPluck()
    {
        if(previouslySpawnedProp != null) Destroy(previouslySpawnedProp.gameObject);
        previouslySpawnedProp = Instantiate(objectToSpawn, transform.position, Quaternion.identity);
        _playerRefData.Lasso.SetupHeldProp(previouslySpawnedProp, null);
    }
}
