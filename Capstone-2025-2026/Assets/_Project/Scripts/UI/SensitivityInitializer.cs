using UnityEngine;
using UnityEngine.UI;

public class SensitivityInitializer : MonoBehaviour
{
    [SerializeField] private Slider xSensitivitySlider;
    [SerializeField] private Slider ySensitivitySlider;

    private void Start()
    {
        CameraRefData.Instance.CameraModeController.SetBaseXSensitivity(xSensitivitySlider.value);
        CameraRefData.Instance.CameraModeController.SetBaseYSensitivity(ySensitivitySlider.value);
    }
}