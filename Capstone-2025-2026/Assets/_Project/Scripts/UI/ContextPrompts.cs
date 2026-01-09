using JetBrains.Annotations;
using UnityEngine;

public class ContextPrompts : MonoBehaviour
{
    public static ContextPrompts Instance;

    [SerializeField] private GameObject player;
    [SerializeField] private GameObject BigMama;
    private LassoTetherController lassoTetherController;
    private JointTetherActivator jointTetherActivator;
    private NPC_Pufferfish npcPufferfish;

    //Looking At Object
    [SerializeField] private GameObject GrabPrompt;

    //Holding Object
    [SerializeField] private GameObject HoldingStateIcons;

    //Rotation Mode
    [SerializeField] private GameObject RotationStateIcons;

    //Tethering Mode
    [SerializeField] private GameObject TetheringStateIcons;

    //Looking at tether
    [SerializeField] private GameObject LookAtTetherPrompts;
    [SerializeField] private GameObject LookAtActiveTetherPrompts; //decided if i make activeate text appear

    //Big Mama
    [SerializeField] private GameObject MamaInBagPrompts;
    [SerializeField] private GameObject MamaInFieldPrompts;

    //Check if children active
    [SerializeField] private ImageFader blackFadeScript;


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

        lassoTetherController = player.GetComponent<LassoTetherController>();
        jointTetherActivator = player.GetComponent<JointTetherActivator>();
        npcPufferfish = BigMama.GetComponent<NPC_Pufferfish>();
    }

    void Update()
    {
        UpdateLassoStateUI();
        UpdateTetherLookUI();
        UpdateNPCUI();
    }

    private void UpdateLassoStateUI()
    {
        HoldingStateIcons.SetActive(false);
        RotationStateIcons.SetActive(false);
        TetheringStateIcons.SetActive(false);

        switch (lassoTetherController.CurrentLassoState)
        {
            case LassoState.Snared:
                HoldingStateIcons.SetActive(true);
                break;

            case LassoState.SnapRotating:
                RotationStateIcons.SetActive(true);
                break;

            case LassoState.Tethering:
            case LassoState.SnaredTether:
                TetheringStateIcons.SetActive(true);
                break;
        }

        if (lassoTetherController.CurrentLassoState != LassoState.Empty)
        {
            blackFadeScript.FadeIn();
        }
    }

    private void UpdateTetherLookUI()
    {
        bool isLooking = jointTetherActivator.isLookingAtTether;
        bool isActiveTether = jointTetherActivator.isLookingAtActiveTether;

        LookAtTetherPrompts.SetActive(isLooking && !isActiveTether);
        LookAtActiveTetherPrompts.SetActive(isLooking && isActiveTether);
    }

    private void UpdateNPCUI()
    {
        bool inBag = npcPufferfish.CurrentNPCState == NPCState.InBag;

        MamaInBagPrompts.SetActive(inBag);
        MamaInFieldPrompts.SetActive(!inBag);
    }

    public void LookingAtObject(bool active)
    {

        if (lassoTetherController.CurrentLassoState == LassoState.Empty)
        {
            GrabPrompt.SetActive(active);
            
            if (active) blackFadeScript.FadeIn();
            else blackFadeScript.FadeOut();

        }
        else
        {
            GrabPrompt.SetActive(false);
        }

    }



}
