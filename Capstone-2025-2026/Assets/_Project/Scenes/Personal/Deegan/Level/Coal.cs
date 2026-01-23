using System.Collections;
using UnityEngine;

public class Coal : Prop
{
    [SerializeField] private float timeToBurnOut = 10f;

    [SerializeField] private Vector3 spawnPosition = Vector3.zero;

    private Coroutine respawnCoroutine = null;

    private void Awake()
    {
        Init();
    }

    private void Start()
    {
       spawnPosition = transform.position;
    }

    protected override void Update()
    {
        base.Update();

        if(IsSnared || IsTetherPulled)
        {
            if(respawnCoroutine == null)
            {
                respawnCoroutine = StartCoroutine(RespawnAfterTime());
            }
        }
    }

    private IEnumerator RespawnAfterTime()
    {
        yield return new WaitForSeconds(timeToBurnOut);

        Respawn();
        respawnCoroutine = null;
    }

    private void Respawn()
    {
        transform.position = spawnPosition;
    }
}
