using UnityEngine;

public class DirtAtLanding : MonoBehaviour
{
    [SerializeField] private GameObject playerDirt;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        playerDirt.SetActive(true);
    }
}
