using UnityEngine;

public class DynamicCrosshair : MonoBehaviour
{
    [SerializeField] private GameObject dotCrosshair;
    [SerializeField] private GameObject crossCrosshair;
    [SerializeField] private JointTetherPlacer tetherPlacer; 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        tetherPlacer.OnPlacementValidityUpdate += UpdateCrosshair;
    }

    private void UpdateCrosshair(bool isTetherValid)
    {
        if (isTetherValid)
        {
            dotCrosshair.SetActive(true);
            crossCrosshair.SetActive(false);
        }
        else
        {
            dotCrosshair.SetActive(false);
            crossCrosshair.SetActive(true);
        }
    }
}
