using UnityEngine;
using UnityEngine.UI;

public class AutoTetherToggle : MonoBehaviour
{
    public PlayerActions playerActionsScript;
    public DevTetherActivationToggle devTetherActivationToggleScript;
    public Image ActivateImage;
    public Sprite AutoOnSprite;
    public Sprite AutoOffSprite;

    private bool ToggleState = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
     if (playerActionsScript.RecallNPCDown)
        {
            devTetherActivationToggleScript.ToggleAutoActivateTether();

            ToggleState = !ToggleState;
        }

        if (ToggleState)
        {
            ActivateImage.sprite = AutoOnSprite;
        }
        else
        {
            ActivateImage.sprite = AutoOffSprite;
        }

    }
}
