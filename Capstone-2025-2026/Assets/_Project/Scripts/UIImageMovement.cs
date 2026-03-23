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
    private GameObject player;
    private LassoTetherController lassoTetherControllerScript;
    private PlayerActions playerActionsScript;
    private RodObtained rodObtainedScript;

    //Sprite and Object References
    //[SerializeField] private GameObject tetherIconObject;
    //[SerializeField] private GameObject toolSwitchIconObject;

    [SerializeField] private RawImage rodImage;
    [SerializeField] private RawImage tetherImage;
    [SerializeField] private RawImage selectionRingImage;
    [SerializeField] private RawImage swapToolImage;


    //Sprites
    public Texture tetherSprite;
    public Texture tetherBrokenSprite; // sprite reference so i can show it being broken and fixed


    //tool locations
    private RectTransform rodLocation;
    private RectTransform tetherLocation;

    private float timeToMove = 0.5f; //time in seconds to move between points


    //TetherCutscene
    public bool tetherCutsceneForceStart = false;
    [SerializeField] private GameObject Tools;

    //Tethertext
    [SerializeField] private TextMeshProUGUI tetherUnlockedText;
    [SerializeField] private TextMeshProUGUI tetherUnlockedDescription;

    

    private void Start()
    {
        //player info
        player = GameObject.FindWithTag("Player");
        rodObtainedScript = GameObject.Find("FindRod").GetComponent<RodObtained>();

        lassoTetherControllerScript = player.GetComponent<LassoTetherController>();
        playerActionsScript = player.GetComponent<PlayerActions>();


        rodLocation = rodImage.GetComponent<RectTransform>();
        tetherLocation = tetherImage.GetComponent<RectTransform>();
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
                selectionRingImage.transform.DOMove(tetherLocation.position, timeToMove, false);
            }
        }
        
        //update UI based on equipped tool
        var lastRodEquipped = lassoTetherControllerScript.rodEquipped;
        if (lassoTetherControllerScript.rodEquipped != lastRodEquipped)
        {
            lastRodEquipped = lassoTetherControllerScript.rodEquipped;

            if (lastRodEquipped)
            {
                RodEquip();
            }
            else
            {
                TetherEquip();
            }
        }

        //check to see if player obtained tether this frame to start cutscene
        if ((lassoTetherControllerScript.tetherPickedUp && rodObtainedScript.tetherObtainedThisFrame) || (tetherCutsceneForceStart))
        {
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

        //start cutscene transition

        //move tools
        Tools.transform.DOLocalMove(new Vector2(-718f, -359f), timeToMove*3, false);
        Tools.transform.DOScale(new Vector3(2, 2f, 1), timeToMove*3);

        //remove other UI
        swapToolImage.DOFade(0, 1);
        selectionRingImage.DOFade(0, 1);


        yield return new WaitForSeconds(3f);

        TetherObtainedSpriteAnimation();


        yield return new WaitForSeconds(1f);

        TetherObtainedText(1);

        yield return new WaitForSeconds(3f);

        TetherObtainedText(0);

        //fade back other UI
        swapToolImage.DOFade(1, timeToMove);
        selectionRingImage.DOFade(1, timeToMove);

        //move tools
        Tools.transform.DOLocalMove(new Vector3(0, 0, 0), timeToMove*3, false);
        Tools.transform.DOScale(new Vector3(1, 1, 1), timeToMove * 3);

        yield return new WaitForSeconds(3f);

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
