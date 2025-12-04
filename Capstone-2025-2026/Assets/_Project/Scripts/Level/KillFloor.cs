using UnityEngine;

public class KillFloor : MonoBehaviour
{
    public Vector3 checkPoint;
    private RespawnPointVisuals respawnPointVisuals;


    public void Start()    
    {
        respawnPointVisuals = GetComponent<RespawnPointVisuals>();
    }

    public void OnTriggerStay(Collider other)
    {

        if (other.gameObject.CompareTag("Player"))
        {

            Transform playerTransform = other.transform;
            Rigidbody playerRB = other.GetComponent<Rigidbody>();
            playerTransform.position = checkPoint;
            playerRB.linearVelocity = Vector3.zero;

        }

    }

}
