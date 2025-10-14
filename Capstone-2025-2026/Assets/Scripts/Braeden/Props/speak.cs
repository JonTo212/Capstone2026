using NodeCanvas.DialogueTrees;
using UnityEngine;

public class speak : MonoBehaviour
{
    public GameObject dialogue;


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            dialogue.SetActive(true);
        }
    }

    private void OnTriggerStay(Collider other)
    {

        if (other.CompareTag("Player"))
        {

            Vector3 target = other.transform.position - transform.position;
            Vector3 newDirection = Vector3.RotateTowards(transform.forward, target, 15f, 0.0f);
            transform.rotation = Quaternion.LookRotation(newDirection);

        }

    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            dialogue.SetActive(false);
        }
    }
}
