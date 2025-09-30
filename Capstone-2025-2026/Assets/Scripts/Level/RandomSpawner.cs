using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomSpawner : MonoBehaviour
{
    [SerializeField] List<GameObject> objectsToSpawn;
    [SerializeField] Vector2 spawnerSize = new Vector2(15,15);
    [SerializeField] int numOfSimultaniousSpawns = 3;
    [SerializeField] float spawnRate = 2f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(SpawnPlatformWave());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator SpawnPlatformWave()
    {
        yield return new WaitForSeconds(1/spawnRate);


    }

    private void SpawnRandomPlatform()
    {
        //get random offset
        float randomXPos = Random.Range(-spawnerSize.x, spawnerSize.x);
        float randomZPos = Random.Range(-spawnerSize.y, spawnerSize.y);

        //get random spawn position
        Vector3 spawnPosition = new Vector3(transform.position.x + randomXPos, transform.position.y, transform.position.z + randomZPos);

        for (int i = 0; i < numOfSimultaniousSpawns; i++)
        {
            //get random object to spawn
            int randomIndex = Random.Range(0, objectsToSpawn.Count);

            //spawn random object at position
        }
        {

        }
    }
}
