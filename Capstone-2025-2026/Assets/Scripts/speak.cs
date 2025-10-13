using UnityEngine;

public class speak : MonoBehaviour
{
    public GameObject dialouge;


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            dialouge.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            dialouge.SetActive(false);
        }
    }
}
