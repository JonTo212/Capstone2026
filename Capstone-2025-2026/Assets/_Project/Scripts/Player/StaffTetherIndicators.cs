using System.Collections.Generic;
using UnityEngine;

public class StaffTetherIndicators : MonoBehaviour
{
    [SerializeField] Transform activeIndicatorsParent;
    [SerializeField] Transform inactiveIndicatorsParent;
    [SerializeField] private List<Transform> activeIndicators;
    [SerializeField] private float debug;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        activeIndicators = new List<Transform>(activeIndicatorsParent.GetComponentsInChildren<Transform>());
        activeIndicators.RemoveAt(0);
        UpdateTetherIndicatorCount(3);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateTetherIndicatorCount(int tethersLeft)
    {
        debug = tethersLeft;

        foreach (Transform t in activeIndicators)
        {
            t.gameObject.SetActive(true);
        }

        for (int i = 0; i < activeIndicators.Count; i++)
        {
            if (i == tethersLeft) return;

            activeIndicators[i].gameObject.SetActive(false);  
        }
    }
}
