using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
public class RodObtained : MonoBehaviour
{
    public LassoTetherController rodController;

    [Header("Give Tool")]
    public ToolEnum selectedTool; // This variable shows in the Inspector
    public enum ToolEnum
    {
        Rod,
        Tether,
    }

    [Header("Getting Rod")]
    public GameObject tutorialText;

    public GameObject rodVisuals;
    public bool rodDisabledFromStart = true;

    [Header("Getting Tether")]
    public GameObject tetherVisuals;
    public bool tetherDisabledFromStart = true;

    private void Start()
    {
        rodController = GameObject.Find("ThirdPersonPlayer").GetComponent<LassoTetherController>();
        tutorialText = GameObject.Find("RodUI");
        rodVisuals = GameObject.Find("fishingRod1");

        if (rodDisabledFromStart)
        {
            DeActivateRod();
        }

        if (tetherDisabledFromStart)
        {
            DeActivateTether();
        }
    }

    public void ActivateRod()
    {
        tutorialText.SetActive(true);
        rodController.rodPickedUp = true;
        rodVisuals.SetActive(true);

        gameObject.SetActive(false);
        AudioManager.Instance.PlaySFX(AudioManager.Instance.RodCollect, 10, 1);

        //InsertVisuals
    }

    public void DeActivateRod()
    {
        tutorialText.SetActive(false);
        rodController.rodPickedUp = false;
        rodVisuals.SetActive(false);
    }

    public void ActivateTether()
    {
        //tutorialText.SetActive(true);
        rodController.tetherPickedUp = true;
        rodVisuals.SetActive(true);

        gameObject.SetActive(false);
        AudioManager.Instance.PlaySFX(AudioManager.Instance.RodCollect, 10, 1);

        //InsertVisuals
    }

    public void DeActivateTether()
    {
        //tutorialText.SetActive(false);
        rodController.tetherPickedUp = false;
        tetherVisuals.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
           if (selectedTool == ToolEnum.Rod) ActivateRod();
           if (selectedTool == ToolEnum.Tether) ActivateTether();
        }
    }
}
