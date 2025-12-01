using NodeCanvas.Tasks.Actions;
using UnityEngine;

public class RespawnPointVisuals : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public bool activated = false;
    public GameObject activeFlag;
    public GameObject inactiveFlag;
    public void SetObjectActive()
    {
        if (!activated)
        {
            print("Activated");
            activated = true;

            activeFlag.SetActive(true);
            inactiveFlag.SetActive(false);

        }
        else
        {
            print("deactivated");
            activated = false;

            activeFlag.SetActive(false);
            inactiveFlag.SetActive(true);
        }
    }
}
