using DG.Tweening;
using FMODUnity;
using System.Collections;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using UnityEngine.UI;


public class UIImageMovement : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private GameObject player;
    [SerializeField] private LassoTetherController lassoTetherControllerScript;

    //[SerializeField] private RodObtained tetherObtainedScript;
    [SerializeField] private RodObtained rodObtainedScript; //must reference the specific prefab instance
    [SerializeField] private RodObtained tetherObtainedScript;

    [SerializeField] private TetherTutorialCutscene tetherTutorialCutsceneScript;
    [SerializeField] private RodTutorialCutscene rodTutorialCutsceneScript;

    
    private bool lastRodEquipped; //used to check if the equipped tool has changed since last frame to update UI


    [Header("Raw Images")]
    [SerializeField] private RawImage rodImage;
    [SerializeField] private RawImage tetherImage;
    [SerializeField] private RawImage selectionRingImage;
    [SerializeField] private RawImage selectionRingButton;
    [SerializeField] private RawImage swapToolImage;
    [SerializeField] private Vector3 swapToolImageStartSize;
    [SerializeField] private RawImage tetherBreakImage;
    [SerializeField] private Vector3 tetherBreakImageStartSize;
    [SerializeField] private RawImage blackBG;



    [Header("Lost Tool Sprites")]// sprite reference so i can show it being broken and fixed
    [SerializeField] private Texture rodSprite;
    [SerializeField] private Texture rodBrokenSprite;

    [SerializeField] private Texture tetherSprite;
    [SerializeField] private Texture tetherBrokenSprite; 


    //tool locations
    private RectTransform rodLocation;
    private RectTransform tetherLocation;


    private float timeToMove = 0.5f; //time in seconds to move between points

    [Header("Tool Cutscene Variables")] //Used for the rod
    public bool RodCutsceneForceStart = false; //used to trigger the cutscene for testing, will be triggered by the rod pickup in the actual game
    public bool tetherCutsceneForceStart = false;

    public float UIFadeTime = 1f;
    public float toolUIMoveTime = 3f;




    [Header("UI Objects")]
    [SerializeField] private GameObject Tools;
    [SerializeField] private GameObject switchTool;
    [SerializeField] private GameObject tetherBreak;


    [Header("RodUnlockText")]
    [SerializeField] private TextMeshProUGUI rodUnlockedText;
    [SerializeField] private TextMeshProUGUI rodUnlockedDescription;

    [Header("TetherUnlockText")]
    [SerializeField] private TextMeshProUGUI tetherUnlockedText;
    [SerializeField] private TextMeshProUGUI tetherUnlockedDescription;
    [SerializeField] private TextMeshProUGUI transformTutorialText;


    [Header("Selection Ring Colors")]
    [SerializeField] private Color rodSelectedColor = new Color(255, 199, 0, 1);
    [SerializeField] private Color tetherSelectedColor = new Color(0, 255, 139,1);


    [Header("Tether Tutorial Cutscene Variables")] //Used for the tether wall puzzle cutscene
    [SerializeField] private Transform cutsceneStartPos;
    [SerializeField] private Transform lookAtTarget;
    [SerializeField] private float cutsceneDuration;
    [SerializeField] private float cutsceneHoldFraction;
    [SerializeField] private float cutsceneBlendInDelay;
    [SerializeField] private float cutsceneBlendInTime;
    [SerializeField] private EndSequeenceTracker endTrack;
    private NPCKeyCutscene spawnCutscene;


    [Header("Active Tethers Count UI")]
    [SerializeField] private GameObject ActiveTetherPrompts;
    private JointTetherActivator jointTetherActivator;



    private void Start()
    {
        //references
        player = GameObject.FindWithTag("Player");

        tetherObtainedScript = GameObject.Find("FindTether").GetComponent<RodObtained>();
        rodObtainedScript = GameObject.Find("FindRod").GetComponent<RodObtained>();

        lassoTetherControllerScript = player.GetComponent<LassoTetherController>();
        rodLocation = rodImage.GetComponent<RectTransform>();
        tetherLocation = tetherImage.GetComponent<RectTransform>();

        //Disable Tool UI at start of game 

        //selection ring
        selectionRingImage.DOFade(0f, 0f);
        selectionRingButton.DOFade(0f, 0f);

        //switch tool
        swapToolImageStartSize = switchTool.transform.localScale;
        switchTool.SetActive(false);
        switchTool.transform.localScale = new Vector3(0, 0, 0);

        //tether break
        tetherBreakImageStartSize = tetherBreakImage.transform.localScale;
        tetherBreak.SetActive(false);
        switchTool.transform.localScale = new Vector3(0, 0, 0);

        //setup where the UI starts selecting first
        lastRodEquipped = !lassoTetherControllerScript.rodEquipped;

        //tether text alpha
        rodUnlockedText.alpha = 0;
        rodUnlockedDescription.alpha = 0;
        tetherUnlockedText.alpha = 0;
        tetherUnlockedDescription.alpha = 0;
        transformTutorialText.alpha = 0;

        //TetherUnlockCutscene
        spawnCutscene = Camera.main.GetComponent<NPCKeyCutscene>();
    }

    void Update()
    {

        if (PlayerActions.Instance.toolSwitchDown)
        {
            //playsound
            RuntimeManager.PlayOneShot("event:/MenuSelect", transform.position);

            //rotate
            RotateIcon();

            //No Tether Yet
            if (!lassoTetherControllerScript.tetherPickedUp)
            {
                //placeholder animation for the switch failing, having issues making the shake work
                selectionRingImage.transform.DOShakePosition(timeToMove, 10, 20, 90, false);
            }
        }
        
        //update UI based on equipped tool

        if (lassoTetherControllerScript.rodEquipped != lastRodEquipped)
        {
            print("tool switch animation");

            if (lastRodEquipped)
            {
                TetherEquip();
                
            }
            else if (lassoTetherControllerScript.rodPickedUp)
            {
                RodEquip();
            }

            lastRodEquipped = lassoTetherControllerScript.rodEquipped; //invert the bool so it only fires logic for 1 frame
        }


        print("rod equipt   " + lastRodEquipped);
;       //check to see if player obtained tether this frame to start cutscene
        if ((tetherObtainedScript.tetherObtainedThisFrame) || (tetherCutsceneForceStart))
        {
            print("TetherCutscene");

            StartCoroutine(TetherObtainedSequence());

            tetherObtainedScript.tetherObtainedThisFrame = false;
            tetherCutsceneForceStart = false;
        }


        if ((rodObtainedScript.rodObtainedThisFrame) || (RodCutsceneForceStart))
        {
            print("Ballright");

            StartCoroutine(RodObtainedSequence());
            rodObtainedScript.rodObtainedThisFrame = false;
            RodCutsceneForceStart = false;
        }

    }


    void RodEquip()
    {
        print("rodEquipt");

        //Change selected tool
        selectionRingImage.transform.DOMove(rodLocation.position, timeToMove, false);

        //Edit Colors
        rodImage.DOColor(new Color(1, 1, 1), timeToMove); //Set Rod to full color
        tetherImage.DOColor(new Color(1, 1, 1, 0.6f), timeToMove); // Set Tether low alpha

        selectionRingImage.DOColor(rodSelectedColor, .1f);
    }

    void TetherEquip()
    {
        if (lassoTetherControllerScript.tetherPickedUp)
        {


            //Change selected tool
            selectionRingImage.transform.DOMove(tetherLocation.position, timeToMove, false);

            //Edit Colors
            rodImage.DOColor(new Color(1, 1, 1, 0.6f), timeToMove);
            tetherImage.DOColor(new Color(1, 1, 1), timeToMove);

            selectionRingImage.DOColor(tetherSelectedColor, .1f);
        }
        else
        {
            print("No Tether Equipt");
        }

    }

    void RotateIcon()
    {
        //swap icon rotate
        swapToolImage.transform.DORotate(new Vector3(0, 0, swapToolImage.transform.rotation.eulerAngles.z - 360f), timeToMove, RotateMode.FastBeyond360);
    }


    #region TetherUnlockSequence
    IEnumerator TetherObtainedSequence()
    {
        //change camera view to look at player
        if (tetherTutorialCutsceneScript != null)
        {
            CameraCutsceneHandler.Instance.StartCutscene(tetherTutorialCutsceneScript);
        }

        //make UI tool controls disapear
        switchTool.transform.DOScale(new Vector3(0, 0, 0), UIFadeTime);
        tetherBreak.transform.DOScale(new Vector3(0, 0, 0), UIFadeTime);
        selectionRingButton.DOFade(.25f, UIFadeTime);

        //black fade
        blackBG.DOFade(.9f, UIFadeTime);

        yield return new WaitForSeconds(UIFadeTime);

        //move tools
        Tools.transform.DOLocalMove(new Vector2(-718f, -359f), timeToMove* toolUIMoveTime, false);
        Tools.transform.DOScale(new Vector3(2, 2f, 1), timeToMove* toolUIMoveTime);

        yield return new WaitForSeconds(toolUIMoveTime);

        //play animation of getting tether
        TetherObtainedSpriteAnimation();

        yield return new WaitForSeconds(1f);

        //"You Got the tether!"
        TetherObtainedText(1);

        yield return new WaitUntil(() => PlayerActions.Instance.JumpDown); //press A to continue

        //fanfare text disapears

        blackBG.DOFade(0f, UIFadeTime);
        TetherObtainedText(0);

        //move tools
        Tools.transform.DOLocalMove(new Vector3(0, 0, 0), timeToMove* toolUIMoveTime, false);
        Tools.transform.DOScale(new Vector3(1, 1, 1), timeToMove * toolUIMoveTime);

        yield return new WaitForSeconds(toolUIMoveTime-1);


        //enable tether
        lassoTetherControllerScript.tetherPickedUp = true;


        //Start QuicktimeEvent
        TransformTutorialText(1);

        yield return new WaitUntil(() => PlayerActions.Instance.toolSwitchDown);

        yield return new WaitForSeconds(1);

        //end cutscene
        tetherTutorialCutsceneScript.EndIndefiniteCutscene();

        //wall cutscene
        spawnCutscene.Configure(cutsceneStartPos, lookAtTarget, cutsceneDuration, cutsceneHoldFraction, cutsceneBlendInDelay, cutsceneBlendInTime);
        CameraCutsceneHandler.Instance.StartCutscene(spawnCutscene);

        //Start QuicktimeEvent
        TransformTutorialText(0);

        //fade back other UI
        switchTool.transform.DOScale(swapToolImageStartSize, UIFadeTime); // slight delay to have it appear after selection ring button. Gives it more character
        selectionRingButton.DOFade(1f, UIFadeTime);

        //add the new tether break button
        tetherBreak.SetActive(true);
        tetherBreak.transform.DOScale(tetherBreakImageStartSize, UIFadeTime + .5f); // slight delay to have it appear after selection ring button. Gives it more character


    }

    public void TetherObtainedSpriteAnimation()
    {
        //update sprite
        //tetherImage.texture = tetherSprite;

        tetherImage.DOFade(1f, 0);
        tetherImage.transform.DOShakePosition(timeToMove, 10, 20, 90, false);


    }

    public void TetherObtainedText(int alphaValue)
    {
        //tetherUnlockedText.SetActive(activeState);//should make it fade niceley

        tetherUnlockedText.DOFade(alphaValue, UIFadeTime);
        tetherUnlockedDescription.DOFade(alphaValue, UIFadeTime * 2);

        RuntimeManager.PlayOneShot("event:/Fanfare", transform.position);
    }

    public void TransformTutorialText(int alphaValue)
    {
        transformTutorialText.DOFade(alphaValue, UIFadeTime);
    }

    #endregion


    #region RodUnlockSequence
    IEnumerator RodObtainedSequence()
    {
        //change camera view to look at player
        if (rodTutorialCutsceneScript != null)
        {
            CameraCutsceneHandler.Instance.StartCutscene(rodTutorialCutsceneScript);
        }


        //black fade
        blackBG.DOFade(.9f, UIFadeTime);

        yield return new WaitForSeconds(UIFadeTime);

        //move tools
        Tools.transform.DOLocalMove(new Vector2(-718f, -359f), toolUIMoveTime, false);
        Tools.transform.DOScale(new Vector3(2, 2f, 1), timeToMove * toolUIMoveTime);

        yield return new WaitForSeconds(toolUIMoveTime);

        //play animation of getting tether
        RodObtainedSpriteAnimation();

        yield return new WaitForSeconds(1f);

        //"You Got the tether!"
        RodObtainedText(1);

        yield return new WaitUntil(() => PlayerActions.Instance.JumpDown); //press A to continue

        //fanfare text disapears

        blackBG.DOFade(0f, UIFadeTime);
        RodObtainedText(0);

        //move tools
        Tools.transform.DOLocalMove(new Vector3(0, 0, 0), timeToMove * toolUIMoveTime, false);
        Tools.transform.DOScale(new Vector3(1, 1, 1), timeToMove * toolUIMoveTime);

        yield return new WaitForSeconds(toolUIMoveTime);

        rodTutorialCutsceneScript.EndIndefiniteCutscene();

        //unlock rod functionality
        //lassoTetherControllerScript.rodPickedUp = true;


        //fade back other UI
        switchTool.SetActive(true);
        switchTool.transform.DOScale(new Vector3(1, 1, 1), UIFadeTime + .5f); // slight delay to have it appear after selection ring button. Gives it more character
        
        selectionRingImage.DOFade(1f, UIFadeTime);
        selectionRingButton.DOFade(1f, UIFadeTime);


    }

    public void RodObtainedSpriteAnimation()
    {
        //update sprite
        //rodImage.texture = rodSprite;

        rodImage.DOFade(1,0);
        rodImage.transform.DOShakePosition(timeToMove, 10, 20, 90, false);
    }

    public void RodObtainedText(int alphaValue)
    {
        rodUnlockedText.DOFade(alphaValue, UIFadeTime);
        rodUnlockedDescription.DOFade(alphaValue, UIFadeTime * 2);

        RuntimeManager.PlayOneShot("event:/Fanfare", transform.position);
    }


    #endregion


}
