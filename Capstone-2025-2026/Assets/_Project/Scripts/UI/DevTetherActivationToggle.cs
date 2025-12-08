using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DevTetherActivationToggle : MonoBehaviour
{
    [SerializeField] JointTetherPlacer jointTetherPlacer;
    [SerializeField] LassoTetherController lassoTetherController;
    [SerializeField] Color onColor = Color.green;
    [SerializeField] Color offColor = Color.red;
    [SerializeField] RectTransform tetherActivationToggleButton;
    [SerializeField] RectTransform tetherModeToggleButton;
    private Image autoActivateTetherImage;
    private Image tetherModeToggleImage;
    private TextMeshProUGUI autoActivateTetherText;
    private TextMeshProUGUI tetherModeToggleText;
    private void Start()
    {
        autoActivateTetherImage = tetherActivationToggleButton.GetComponent<Image>();
        autoActivateTetherText = tetherActivationToggleButton.GetChild(0).GetComponent<TextMeshProUGUI>();

        tetherModeToggleImage = tetherModeToggleButton.GetComponent<Image>();
        tetherModeToggleText = tetherModeToggleButton.GetChild(0).GetComponent<TextMeshProUGUI>();

        SetAutoActivateButtonColor();
        SetTetherModeButtonColor();
    }

    public void ToggleAutoActivateTether()
    {
        jointTetherPlacer.ToggleAutoActivateTether();
        SetAutoActivateButtonColor();
    }

    public void ToggletTetherMode()
    {
        lassoTetherController.ToggleAlowTetherModeActivation();
        SetTetherModeButtonColor();
    }

    private void SetAutoActivateButtonColor()
    {
        if(jointTetherPlacer.autoActivateTether)
        {
            autoActivateTetherImage.color = onColor;
            autoActivateTetherText.SetText("Auto Activate Tether: On");
        }
        else
        {
            autoActivateTetherImage.color = offColor;
            autoActivateTetherText.SetText("Auto Activate Tether: Off");
        }
    }

    private void SetTetherModeButtonColor()
    {
        if (lassoTetherController.TetherMode)
        {
            tetherModeToggleImage.color = onColor;
            tetherModeToggleText.SetText("Tether Mode: On");
        }
        else
        {
            tetherModeToggleImage.color = offColor;
            tetherModeToggleText.SetText("Tether Mode: Off");
        }
    }
}
