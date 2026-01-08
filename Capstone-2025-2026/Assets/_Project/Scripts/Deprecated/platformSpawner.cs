using UnityEngine;

public class platformSpawner : MonoBehaviour
{
    public GameObject platformHolder;

    public bool invertSignal = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!invertSignal) platformHolder.SetActive(true);
        else platformHolder.SetActive(false);


    }

    private void OnTriggerExit(Collider other)
    {
        if (!invertSignal) platformHolder.SetActive(false);
        else platformHolder.SetActive(true);
    }
}
