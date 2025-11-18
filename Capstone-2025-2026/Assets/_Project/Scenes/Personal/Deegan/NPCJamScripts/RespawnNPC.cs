using UnityEngine;

public class RespawnNPC : MonoBehaviour
{
    private Vector3 spawnPosition;
    public Transform spawnTransform;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(spawnTransform != null)
        {
            spawnPosition = spawnTransform.position;
        }
        else
        {
            spawnPosition = transform.position;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(transform.position.y < -15f)
        {
            transform.position = spawnPosition;

            if(transform.GetComponent<Rigidbody>() != null )
            {
                transform.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
            }
        }
    }
}
