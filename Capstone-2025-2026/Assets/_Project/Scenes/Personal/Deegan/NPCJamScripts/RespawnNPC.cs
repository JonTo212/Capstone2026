using UnityEngine;

public class RespawnNPC : MonoBehaviour
{
    private Vector3 spawnPosition;
    [SerializeField] private Quaternion spawnRotation;

    public Transform spawnTransform;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if(spawnTransform != null)
        {
            spawnPosition = spawnTransform.position;
            spawnRotation = spawnTransform.rotation;

        }
        else
        {
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
        }
    }

    // Update is called once per frame
    void Update()
    {
        print(spawnRotation);

        if(transform.position.y < -30f)
        {
            transform.position = spawnPosition;
            transform.rotation = spawnRotation;

            if (transform.GetComponent<Rigidbody>() != null )
            {
                transform.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
                transform.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
            }
        }
    }
}
