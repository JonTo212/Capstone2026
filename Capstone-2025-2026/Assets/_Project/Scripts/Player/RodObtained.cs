using FMODUnity;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
public class RodObtained : MonoBehaviour
{
    //THIS SCRIPT IS WORKING WITH UIIMAGEMOVEMENT SCRIPT TO TELL IT WHEN TO ENABLE THE TOOLS AND START THE CUTSCENES.

    [SerializeField] private GameObject player;
    [SerializeField] private LassoTetherController lassoTetherControllerScript;
    [SerializeField] private bool toolUnlockedFromStart = false;
    private GameObject selectedModel; //tool model that will display in the ground

    [Header("Tool to Give")]

    public ToolEnum selectedTool; // This variable shows in the Inspector
    public enum ToolEnum
    {
        Rod,
        Tether,
    }

    [Header("Rod")]
    [SerializeField] private GameObject rodPlayerHandModel;
    [SerializeField] private GameObject rodDummyModel;
    [SerializeField] public bool rodObtainedThisFrame; // checked in 1 frame so the cutscene only triggers once

    [Header("Tether")]
    [SerializeField] private GameObject tetherDummyModel;
    [SerializeField] public bool tetherObtainedThisFrame; // checked in 1 frame so the cutscene only triggers once

    [SerializeField] private Collider col;

    public void Awake() 
    {
        //get dependencies
        player = GameObject.FindWithTag("Player");
        lassoTetherControllerScript = player.GetComponent<LassoTetherController>();
        col = GetComponent<SphereCollider>();


        //Switch out models depending on what tool is selected 
        if (rodPlayerHandModel != null) rodPlayerHandModel.SetActive(false);
        rodDummyModel.SetActive(false);
        tetherDummyModel.SetActive(false);
        

        if (selectedTool == ToolEnum.Rod) selectedModel = rodDummyModel;
        if (selectedTool == ToolEnum.Tether) selectedModel = tetherDummyModel;

        //change setup depeing on if the tool is unlocked from the start or not 
        if (toolUnlockedFromStart)
        {
            //disable model since its already been picked up
            if (selectedModel != null) selectedModel.SetActive(false);


            //disable the non-selected tools logic
            if (selectedTool != ToolEnum.Rod) DeActivateRod();
            if (selectedTool != ToolEnum.Tether) DeActivateTether();
        }
        else
        {

            //enable model ready to be obtained
            if (selectedModel != null) selectedModel.SetActive(true);

            //disable all tool logic
            DeActivateRod();
            DeActivateTether();
        }
    }

    public void ActivateRod()
    {
        rodObtainedThisFrame = true;

        if (lassoTetherControllerScript == null)
        {
            Debug.LogWarning("No Rod Controller Found");
            return;
        }

        // Enable rod functionality
        lassoTetherControllerScript.rodPickedUp = true;

        if (rodPlayerHandModel!=null) rodPlayerHandModel.SetActive(true); // make tool appear in players hand
        if (rodDummyModel != null) rodDummyModel.SetActive(false); // make tool in ground disapear

        //fanfare sfx 
        RuntimeManager.PlayOneShot("event:/Fanfare", transform.position);
    }

    public void DeActivateRod()
    {
        lassoTetherControllerScript.rodPickedUp = false;
    }

    public void ActivateTether()
    {
        //tells the UIIMAGEMOVEMENT script to start cutscene
        tetherObtainedThisFrame = true;

        //Disable Dummy model 
        if (tetherDummyModel != null) tetherDummyModel.SetActive(false); //i think this can be deleted becuase there is no longer 2 seperate models

        //fanfare sfx
        RuntimeManager.PlayOneShot("event:/Fanfare", transform.position);
    }

    public void DeActivateTether()
    {
        lassoTetherControllerScript.tetherPickedUp = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
           if (selectedTool == ToolEnum.Rod) ActivateRod();

            if (selectedTool == ToolEnum.Tether)
            {
                ActivateTether();

                //GetComponent<DialogueTrigger>().CreateNPCDialogue();
            }

            col.enabled = false; //disable collider so it cant be obtained again
        }
    }
}
