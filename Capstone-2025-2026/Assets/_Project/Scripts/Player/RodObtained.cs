using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
public class RodObtained : MonoBehaviour
{
    public GameObject tutorialText;
    public LassoTetherController rodController;
    public GameObject rodVisuals;
    public bool disabledFromStart = true;

    private void Start()
    {
        rodController = GameObject.Find("ThirdPersonPlayer").GetComponent<LassoTetherController>();
        tutorialText = GameObject.Find("PlayerUICanvas");
        rodVisuals = GameObject.Find("fishingRod1");

        if (disabledFromStart)
        {
            DeActivateRod();
        }
    }

    public void ActivateRod()
    {
        tutorialText.SetActive(true);
        rodController.enabled = true;
        rodVisuals.SetActive(true);
        //InsertVisuals
    }

    public void DeActivateRod()
    {
        tutorialText.SetActive(false);
        rodController.enabled = false;
        rodVisuals.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ActivateRod();
        }
    }
}
