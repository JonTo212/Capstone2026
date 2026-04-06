using DG.Tweening;
using FMODUnity;
using System;
using System.Collections;
using TMPro;
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

    private Vector3 rodImageStartPos;
    private Vector3 tetherImageStartPos;


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

        rodImageStartPos = rodImage.transform.localPosition;
        tetherImageStartPos = tetherImage.transform.localPosition;
    }

    void Update()
    {

        if (PlayerActions.Instance.toolSwitchDown)
        {
            //playsound
            RuntimeManager.PlayOneShot("event:/ToolSwitch", transform.position);

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

        //check to see if player obtained tether this frame to start cutscene
        if ((tetherObtainedScript.tetherObtainedThisFrame) || (tetherCutsceneForceStart))
        {
            StartCoroutine(TetherObtainedSequence());

            tetherObtainedScript.tetherObtainedThisFrame = false;
            tetherCutsceneForceStart = false;
        }


        if ((rodObtainedScript.rodObtainedThisFrame) || (RodCutsceneForceStart))
        {
            StartCoroutine(RodObtainedSequence());
            rodObtainedScript.rodObtainedThisFrame = false;
            RodCutsceneForceStart = false;
        }

    }


    void RodEquip()
    {
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
        // Start camera cutscene
        if (tetherTutorialCutsceneScript != null)
            CameraRefData.Instance.CameraCutsceneHandler.StartCutscene(tetherTutorialCutsceneScript);

        // Fully skippable intro block
        yield return StartCoroutine(PlayTetherIntroBlock());

        // Wait for player to continue
        yield return new WaitUntil(() => PlayerActions.Instance.JumpDown);

        // Fully skippable outro block
        yield return StartCoroutine(PlayTetherOutroBlock());

        // Finalize cutscene
        FinishTetherCutscene();
    }

    IEnumerator PlayTetherIntroBlock()
    {
        // Hide UI tool controls
        switchTool.transform.DOScale(Vector3.zero, UIFadeTime);
        tetherBreak.transform.DOScale(Vector3.zero, UIFadeTime);
        selectionRingButton.DOFade(.25f, UIFadeTime);

        // Fade in black
        blackBG.DOFade(.9f, UIFadeTime);

        yield return StartCoroutine(WaitOrSkip(UIFadeTime, () =>
        {
            DOTween.Kill(blackBG);
            blackBG.DOFade(.9f, 0f);

            DOTween.Kill(switchTool.transform);
            DOTween.Kill(tetherBreak.transform);
            DOTween.Kill(selectionRingButton);

            switchTool.transform.DOScale(Vector3.zero, 0f);
            tetherBreak.transform.DOScale(Vector3.zero, 0f);
            selectionRingButton.DOFade(.25f, 0f);
        }));


        // Move tools
        Tools.transform.DOLocalMove(new Vector2(-718f, -359f), toolUIMoveTime, false);
        Tools.transform.DOScale(new Vector3(2, 2f, 1), toolUIMoveTime);

        yield return StartCoroutine(WaitOrSkip(toolUIMoveTime, () =>
        {
            DOTween.Kill(Tools.transform);

            Tools.transform.DOLocalMove(new Vector2(-718f, -359f), 0f);
            Tools.transform.DOScale(new Vector3(2, 2f, 1), 0f);
        }));


        // Shake animation
        TetherObtainedSpriteAnimation();

        yield return StartCoroutine(WaitOrSkip(1f, () =>
        {
            DOTween.Kill(tetherImage);
            DOTween.Kill(tetherImage.transform);

            tetherImage.DOFade(1f, 0f);
            tetherImage.transform.DOLocalMove(tetherImageStartPos, 0f); // adjust if needed
        }));


        // Text fade-in
        TetherObtainedText(1);
        RuntimeManager.PlayOneShot("event:/Fanfare", transform.position);

        yield return StartCoroutine(WaitOrSkip(UIFadeTime, () =>
        {
            DOTween.Kill(tetherUnlockedText);
            DOTween.Kill(tetherUnlockedDescription);

            tetherUnlockedText.DOFade(1f, 0f);
            tetherUnlockedDescription.DOFade(1f, 0f);
        }));
    }

    IEnumerator PlayTetherOutroBlock()
    {
        // Fade out black + text
        blackBG.DOFade(0f, UIFadeTime);
        TetherObtainedText(0);

        // Move tools back
        Tools.transform.DOLocalMove(Vector3.zero, toolUIMoveTime, false);
        Tools.transform.DOScale(Vector3.one, toolUIMoveTime);

        yield return StartCoroutine(WaitOrSkip(toolUIMoveTime, () =>
        {
            DOTween.Kill(blackBG);
            DOTween.Kill(tetherUnlockedText);
            DOTween.Kill(tetherUnlockedDescription);
            DOTween.Kill(Tools.transform);

            blackBG.DOFade(0f, 0f);
            tetherUnlockedText.DOFade(0f, 0f);
            tetherUnlockedDescription.DOFade(0f, 0f);

            Tools.transform.DOLocalMove(Vector3.zero, 0f);
            Tools.transform.DOScale(Vector3.one, 0f);
        }));
    }

    void FinishTetherCutscene()
    {
        // Enable tether
        lassoTetherControllerScript.tetherPickedUp = true;

        // Start quick-time prompt
        TransformTutorialText(1);

        // Wait for tool switch
        StartCoroutine(WaitForToolSwitch());
    }

    IEnumerator WaitForToolSwitch()
    {
        yield return new WaitUntil(() => PlayerActions.Instance.toolSwitchDown);
        yield return new WaitForSeconds(1);

        // End cutscene
        tetherTutorialCutsceneScript.EndIndefiniteCutscene();

        // Wall cutscene
        spawnCutscene.Configure(cutsceneStartPos, lookAtTarget, cutsceneDuration, cutsceneHoldFraction, cutsceneBlendInDelay, cutsceneBlendInTime);
        CameraRefData.Instance.CameraCutsceneHandler.StartCutscene(spawnCutscene);

        // Hide tutorial text
        TransformTutorialText(0);

        // Restore UI
        switchTool.transform.DOScale(swapToolImageStartSize, UIFadeTime);
        selectionRingButton.DOFade(1f, UIFadeTime);

        tetherBreak.SetActive(true);
        tetherBreak.transform.DOScale(tetherBreakImageStartSize, UIFadeTime + .5f);
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
    }

    public void TransformTutorialText(int alphaValue)
    {
        transformTutorialText.DOFade(alphaValue, UIFadeTime);
    }

    #endregion


    #region RodUnlockSequence

    IEnumerator RodObtainedSequence()
    {
        if (rodTutorialCutsceneScript != null)
            CameraRefData.Instance.CameraCutsceneHandler.StartCutscene(rodTutorialCutsceneScript);

        //skippable intro block
        yield return StartCoroutine(PlayIntroBlock());

        //press A to continue
        yield return new WaitUntil(() => PlayerActions.Instance.JumpDown);

        //skippable outro block
        yield return StartCoroutine(PlayOutroBlock());

        //end cutscene
        RunRodCutsceneEnd();
    }



    IEnumerator PlayIntroBlock()
    {
        //fade in black background
        blackBG.DOFade(.9f, UIFadeTime);

        yield return StartCoroutine(WaitOrSkip(UIFadeTime, () =>
        {
            DOTween.Kill(blackBG);
            blackBG.DOFade(.9f, 0f);
        }));


        //move tool ui
        Tools.transform.DOLocalMove(new Vector2(-718f, -359f), toolUIMoveTime, false);
        Tools.transform.DOScale(new Vector3(2, 2f, 1), toolUIMoveTime);

        yield return StartCoroutine(WaitOrSkip(toolUIMoveTime, () =>
        {
            DOTween.Kill(Tools.transform);

            Tools.transform.DOLocalMove(new Vector2(-718f, -359f), 0f);
            Tools.transform.DOScale(new Vector3(2, 2f, 1), 0f);
        }));


        //anim
        RodObtainedSpriteAnimation();

        yield return StartCoroutine(WaitOrSkip(1f, () =>
        {
            DOTween.Kill(rodImage);
            DOTween.Kill(rodImage.transform);

            rodImage.DOFade(1f, 0f);
            rodImage.transform.DOLocalMove(rodImageStartPos, 0f);
        }));

        //fade in text
        RodObtainedText(1);
        RuntimeManager.PlayOneShot("event:/Fanfare", transform.position);

        yield return StartCoroutine(WaitOrSkip(UIFadeTime, () =>
        {
            DOTween.Kill(rodUnlockedText);
            DOTween.Kill(rodUnlockedDescription);

            rodUnlockedText.DOFade(1f, 0f);
            rodUnlockedDescription.DOFade(1f, 0f);
        }));
    }

    IEnumerator PlayOutroBlock()
    {
        //fade out black tint + text
        blackBG.DOFade(0f, UIFadeTime);
        var (textTween, descTween) = RodObtainedText(0);

        //move tool ui back
        Tools.transform.DOLocalMove(Vector3.zero, toolUIMoveTime, false);
        Tools.transform.DOScale(Vector3.one, toolUIMoveTime);

        yield return StartCoroutine(WaitOrSkip(toolUIMoveTime, () =>
        {
            //kill tweens
            DOTween.Kill(blackBG);
            DOTween.Kill(rodUnlockedText);
            DOTween.Kill(rodUnlockedDescription);
            DOTween.Kill(Tools.transform);

            //run the same tweens but with 0 duration for instant finish
            blackBG.DOFade(0f, 0f);
            rodUnlockedText.DOFade(0f, 0f);
            rodUnlockedDescription.DOFade(0f, 0f);

            Tools.transform.DOLocalMove(Vector3.zero, 0f);
            Tools.transform.DOScale(Vector3.one, 0f);
        }));
    }



    private void RunRodCutsceneEnd()
    {
        rodTutorialCutsceneScript.EndIndefiniteCutscene();

        //unlock rod functionality
        //lassoTetherControllerScript.rodPickedUp = true;


        //fade back other UI
        switchTool.SetActive(true);
        switchTool.transform.DOScale(new Vector3(1, 1, 1), UIFadeTime + .5f); // slight delay to have it appear after selection ring button. Gives it more character

        selectionRingImage.DOFade(1f, UIFadeTime);
        selectionRingButton.DOFade(1f, UIFadeTime);
    }

    public Tween RodObtainedSpriteAnimation()
    {
        rodImage.DOFade(1, 0); // instant fade-in

        // return the shake tween so we can kill it on skip
        return rodImage.transform.DOShakePosition(timeToMove, strength: 10, vibrato: 20, randomness: 90, snapping: false);
    }


    public (Tween textTween, Tween descTween) RodObtainedText(int alphaValue)
    {
        var t1 = rodUnlockedText.DOFade(alphaValue, UIFadeTime);
        var t2 = rodUnlockedDescription.DOFade(alphaValue, UIFadeTime * 2);

        return (t1, t2);
    }



    #endregion

    #region Skip Helper

    IEnumerator WaitOrSkip(float duration, Action onSkip)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (PlayerActions.Instance.JumpDown)
            {
                onSkip?.Invoke();
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }



    #endregion

}
