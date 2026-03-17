using UnityEngine;

public class tetherSnareTutorial : MonoBehaviour
{
    private LassoTetherController tetherControllerScript;
    public GameObject tetherTutorialCanvas;

    private void Start()
    {
        tetherControllerScript = GameObject.FindWithTag("Player").GetComponent<LassoTetherController>();

        tetherTutorialCanvas.SetActive(false); // start hidden
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (tetherControllerScript.rodEquipped)
            {
                tetherTutorialCanvas.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            tetherTutorialCanvas.SetActive(false);
        }
    }
}