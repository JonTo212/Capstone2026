using JetBrains.Annotations;
using UnityEngine;

public class ContextPrompts : MonoBehaviour
{
    public static ContextPrompts Instance;

    public GameObject player;
    public GameObject BigMama;

    //Looking At Object
    public GameObject GrabPrompt;

    //Holding Object
    public GameObject HoldingStateIcons;

    //Rotation Mode
    public GameObject RotationStateIcons;

    //Tethering Mode
    public GameObject TetheringStateIcons;

    //Looking at tether
    public GameObject LookAtTetherPrompts;
    public GameObject LookAtActiveTetherPrompts; //decided if i make activeate text appear

    //Big Mama
    public GameObject MamaInBagPrompts;
    public GameObject MamaInFieldPrompts;

    //Check if children active
    public bool anyChildrenActive = false;
    public BlackPromptBGFade blackFadeScript;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //Holding objects
        if (player.GetComponent<LassoTetherController>().CurrentLassoState == LassoState.Snared)
        {
            HoldingStateIcons.SetActive(true);
        }
        else
        {
            HoldingStateIcons.SetActive(false);
        }



        //Rotation Mode
        if (player.GetComponent<LassoTetherController>().CurrentLassoState == LassoState.SnapRotating)
        {
            RotationStateIcons.SetActive(true);
            //LookingAtObject(false);
        }
        else 
        {
            RotationStateIcons.SetActive(false);
        }
       

        //Creating Tether
        if ((player.GetComponent<LassoTetherController>().CurrentLassoState == LassoState.Tethering) || (player.GetComponent<LassoTetherController>().CurrentLassoState == LassoState.SnaredTether))
        {
            TetheringStateIcons.SetActive(true);
            //LookingAtObject(false);
        }
        else
        {
            TetheringStateIcons.SetActive(false);
        }

        //Lookat Tether
        if (player.GetComponent<JointTetherActivator>().isLookingAtTether)
        {
            if (player.GetComponent<JointTetherActivator>().isLookingAtActiveTether)
            {
                LookAtActiveTetherPrompts.SetActive(true);
            }
            else
            {
                LookAtTetherPrompts.SetActive(true);
            }

        }
        else
        {
            LookAtTetherPrompts.SetActive(false);
            LookAtActiveTetherPrompts.SetActive(false);
        }



        //Big Mama (needs to only appear once she becomes your friend)
        if (BigMama.GetComponent<NPC_Pufferfish>().CurrentNPCState == NPCState.InBag)
        {
            MamaInBagPrompts.SetActive(true);
            MamaInFieldPrompts.SetActive(false);

        }
        else
        {
            MamaInBagPrompts.SetActive(false);
            MamaInFieldPrompts.SetActive(true);
        }

        //black BG
        if (player.GetComponent<LassoTetherController>().CurrentLassoState != LassoState.Empty)
        {
            blackFadeScript.FadeIn();
        }

    }


    //Looking at an object prompt
    public void LookingAtObject(bool active)
    {

        if (player.GetComponent<LassoTetherController>().CurrentLassoState == LassoState.Empty)
        {
            GrabPrompt.SetActive(active);
            
            if (active == true) blackFadeScript.FadeIn();
            else blackFadeScript.FadeOut();

        }
        else
        {
            GrabPrompt.SetActive(false);
        }

    }



}
