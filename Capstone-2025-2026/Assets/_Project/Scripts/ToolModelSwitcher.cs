using UnityEngine;

public class ToolModelSwitcher : MonoBehaviour
{
    public GameObject HeldRod;
    public GameObject HeldLasso;

    private LassoTetherController lassoTetherControllerScript;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lassoTetherControllerScript = GetComponent<LassoTetherController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (lassoTetherControllerScript.rodEquipped)
        {
            HeldRod.SetActive(true);
            HeldLasso.SetActive(false);
        }
        else
        {
            HeldRod.SetActive(false);
            HeldLasso.SetActive(true);
        }
    }
}
