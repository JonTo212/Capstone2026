using UnityEngine;

public class Respawn : MonoBehaviour
{
    public Transform respawnLocation;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Player")
        {
            other.transform.root.position = transform.position;
        }
    }
}
