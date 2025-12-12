using UnityEngine;

public class tutorialTrigger : MonoBehaviour
{
    public GameObject promptUI;
    public PlayerActions playerActions;
    private bool playerInside = false;

    private void Start()
    {
        if (promptUI != null)
            promptUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            if (promptUI != null)
                promptUI.SetActive(true);
        }
    }

    private void Update()
    {
        if (playerInside && playerActions.LassoDown)
        {
            promptUI.SetActive(false);
            Destroy(gameObject);
        }
    }
}
