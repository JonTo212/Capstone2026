using DG.Tweening;
using FMODUnity;
using System.Collections;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using UnityEngine.UI;


public class UIImageMovement : MonoBehaviour
{

    //References
    [SerializeField] private GameObject player;
    [SerializeField] private LassoTetherController lassoTetherControllerScript;
    [SerializeField] private PlayerActions playerActionsScript;
    [SerializeField] private RodObtained rodObtainedScript;

    //Sprite and Object References
    //[SerializeField] private GameObject tetherIconObject;
    //[SerializeField] private GameObject toolSwitchIconObject;

    [SerializeField] private RawImage rodImage;
    [SerializeField] private RawImage tetherImage;
    [SerializeField] private RawImage selectionRingImage;
    [SerializeField] private RawImage selectionRingButton;
    [SerializeField] private RawImage swapToolImage;


    //Sprites
    public Texture tetherSprite;
    public Texture tetherBrokenSprite; // sprite reference so i can show it being broken and fixed


    //tool locations
    private RectTransform rodLocation;
    private RectTransform tetherLocation;

    private float timeToMove = 0.5f; //time in seconds to move between points


    //UI objects
    [SerializeField] private GameObject Tools;
    [SerializeField] private GameObject OtherControls;

    //TetherCutscene
    public bool tetherCutsceneForceStart = false;

    public float UIFadeTime = 1f;
    public float toolUIMoveTime = 3f;


    //Tethertext
    [SerializeField] private TextMeshProUGUI tetherUnlockedText;
    [SerializeField] private TextMeshProUGUI tetherUnlockedDescription;

    private bool lastRodEquipped; //used to check if the equipped tool has changed since last frame to update UI

    private void Start()
    {
        //player info
        player = GameObject.FindWithTag("Player");
        rodObtainedScript = GameObject.Find("FindTether").GetComponent<RodObtained>();

        lassoTetherControllerScript = player.GetComponent<LassoTetherController>();
        playerActionsScript = player.GetComponent<PlayerActions>();


        rodLocation = rodImage.GetComponent<RectTransform>();
        tetherLocation = tetherImage.GetComponent<RectTransform>();


        lastRodEquipped = !lassoTetherControllerScript.rodEquipped;
    }

    void Update()
    {
        if (playerActionsScript.toolSwitchDown)
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
                RodEquip();
            }
            else
            {
                TetherEquip();
            }

            lastRodEquipped = lassoTetherControllerScript.rodEquipped; //invert the bool so it only fires logic for 1 frame
        }


        print("rod equipt   " + lastRodEquipped);
;       //check to see if player obtained tether this frame to start cutscene
        if ((lassoTetherControllerScript.tetherPickedUp && rodObtainedScript.tetherObtainedThisFrame) || (tetherCutsceneForceStart))
        {
            print("TetherCutscene");

            StartCoroutine(TetherObtainedSequence());

            rodObtainedScript.tetherObtainedThisFrame = false;
            tetherCutsceneForceStart = false;
        }
    }


    void RodEquip()
    {
        //Change selected tool
        selectionRingImage.transform.DOMove(rodLocation.position, timeToMove, false);

        //Edit Colors
        rodImage.DOColor(new Color(1, 1, 1), timeToMove); //Set Rod to full color
        tetherImage.DOColor(new Color(1, 1, 1, 0.6f), timeToMove); // Set Tether low alpha
        selectionRingImage.DOColor(new Color(0, 255, 139), .1f);
    }

    void TetherEquip()
    {
        if (lassoTetherControllerScript.tetherPickedUp)
        {


            //Change selected tool
            selectionRingImage.transform.DOMove(tetherLocation.position, timeToMove, false);

            //Edit Colors
            selectionRingImage.DOColor(new Color(255, 199, 0), .1f);
            rodImage.DOColor(new Color(1, 1, 1, 0.6f), timeToMove);
            tetherImage.DOColor(new Color(1, 1, 1), timeToMove);
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

        //play different animation if no tether equipped

    }

    IEnumerator TetherObtainedSequence()
    {
        selectionRingImage.DOFade(0, UIFadeTime);
        selectionRingButton.DOFade(0, UIFadeTime);

        OtherControls.transform.DOScale(new Vector3(0, 0, 0), UIFadeTime);


        //OtherControls.SetActive(false);

        yield return new WaitForSeconds(UIFadeTime);

        //move tools
        Tools.transform.DOLocalMove(new Vector2(-718f, -359f), timeToMove* toolUIMoveTime, false);
        Tools.transform.DOScale(new Vector3(2, 2f, 1), timeToMove* toolUIMoveTime);



        yield return new WaitForSeconds(toolUIMoveTime);

        TetherObtainedSpriteAnimation();


        yield return new WaitForSeconds(1f);

        TetherObtainedText(1);

        yield return new WaitForSeconds(toolUIMoveTime);

        TetherObtainedText(0);


        //move tools
        Tools.transform.DOLocalMove(new Vector3(0, 0, 0), timeToMove* toolUIMoveTime, false);
        Tools.transform.DOScale(new Vector3(1, 1, 1), timeToMove * toolUIMoveTime);

        yield return new WaitForSeconds(toolUIMoveTime-2);

        //fade back other UI
        OtherControls.transform.DOScale(new Vector3(1, 1, 1), UIFadeTime);

        selectionRingImage.transform.position = rodLocation.position;
        selectionRingImage.DOFade(1, 1);
        selectionRingButton.DOFade(1, 1);

    }

    public void TetherObtainedSpriteAnimation()
    {
        //update sprite
        tetherImage.texture = tetherSprite;
        tetherImage.transform.DOShakePosition(timeToMove, 10, 20, 90, false);
    }

    public void TetherObtainedText(int alphaValue)
    {
        //tetherUnlockedText.SetActive(activeState);//should make it fade niceley

        tetherUnlockedText.DOFade(alphaValue, timeToMove);
        tetherUnlockedDescription.DOFade(alphaValue, timeToMove*2);
    }

}
