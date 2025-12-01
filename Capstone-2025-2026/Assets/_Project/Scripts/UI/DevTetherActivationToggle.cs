using UnityEngine;
using UnityEngine.UI;

public class DevTetherActivationToggle : MonoBehaviour
{
    [SerializeField] JointTetherPlacer jointTetherPlacer;
    [SerializeField] RectTransform tetherActivationToggleButton;
    [SerializeField] Color autoActivateOn = Color.green;
    [SerializeField] Color autoActivateOff = Color.red;
    private Image toggleButtonImage; 
    private TMPro.TextMeshProUGUI text;
    private void Start()
    {
        toggleButtonImage = tetherActivationToggleButton.GetComponent<Image>();
        text = tetherActivationToggleButton.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>();
        SetButtonColor();
    }

    public void ToggleAutoActivateTether()
    {
        jointTetherPlacer.ToggleAutoActivateTether();
        SetButtonColor();
    }

    private void SetButtonColor()
    {
        if(jointTetherPlacer.autoActivateTether)
        {
            toggleButtonImage.color = autoActivateOn;
            text.SetText("Auto Activate Tether: On");
        }
        else
        {
            toggleButtonImage.color = autoActivateOff;
            text.SetText("Auto Activate Tether: Off");
        }
    }
}
