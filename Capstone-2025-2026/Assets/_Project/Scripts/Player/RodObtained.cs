using FMODUnity;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
public class RodObtained : MonoBehaviour
{

    //!rodEquipped
    [SerializeField] private LassoTetherController lassoTetherControllerScript;

    [Header("Tool to Give")]

    public ToolEnum selectedTool; // This variable shows in the Inspector
    public enum ToolEnum
    {
        Rod,
        Tether,
    }

    [Header("Rod")]
    [SerializeField] private GameObject rodModel;
    [SerializeField] private GameObject toolUI;

    public bool rodDisabledFromStart = true;


    [Header("Tether")]
    [SerializeField] private GameObject tetherModel;
    public bool tetherDisabledFromStart = true;


    public void Awake() 
    {
        //get depe
        lassoTetherControllerScript = GameObject.Find("ThirdPersonPlayer").GetComponent<LassoTetherController>();
        toolUI = GameObject.Find("RodUI");
        rodModel = GameObject.Find("NewTool");
        //tetherModel = GameObject.Find("NewToolGrapple");

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
        print("rod obtained");

  
        //check if everything is here
        if (toolUI == null)
        {
            Debug.LogWarning("No Tool UI Found");
            return;
        }

        if (lassoTetherControllerScript == null)
        {
            Debug.LogWarning("No Rod Controller Found");
            return;
        }

        if (rodModel == null)
        {
            Debug.LogWarning("No Rod Model Found");
            return;
        }

        // Enable tool UI
        toolUI.SetActive(true);

        // Enable rod functionality
        lassoTetherControllerScript.rodPickedUp = true;
        if (rodModel != null) rodModel.SetActive(true);

        //play sfx and disable game object
        //AudioManager.Instance.PlaySFX(AudioManager.Instance.RodCollect, 10, 1);
        RuntimeManager.PlayOneShot("event:/Fanfare", transform.position);
        gameObject.SetActive(false);

    }

    public void DeActivateRod()
    {
        toolUI.SetActive(false);
        lassoTetherControllerScript.rodPickedUp = false;
        if (rodModel != null) rodModel.SetActive(false);
    }

    public void ActivateTether()
    {
        //tutorialText.SetActive(true);
        lassoTetherControllerScript.tetherPickedUp = true;
        lassoTetherControllerScript.rodEquipped = false;

        if (tetherModel!=null) tetherModel.SetActive(true);

        gameObject.SetActive(false);
        //AudioManager.Instance.PlaySFX(AudioManager.Instance.RodCollect, 10, 1);
        RuntimeManager.PlayOneShot("event:/Fanfare", transform.position);

        //InsertVisuals
    }

    public void DeActivateTether()
    {
        lassoTetherControllerScript.tetherPickedUp = false;
        if (tetherModel != null) tetherModel.SetActive(false);
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
