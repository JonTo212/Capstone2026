using UnityEngine;

public class ContextPrompts : MonoBehaviour
{
    public GameObject player;

    //Looking At Object
    public GameObject GrabPrompt;

    //Holding Object
    public GameObject HoldingStateIcons;

    //Rotation Mode
    public GameObject RotationStateIcons;

    //Big Mama






    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //Holding objects
        if (player.GetComponent<LassoTetherController>().CurrentLassoState == LassoState.Snared)
        {
            //StartTetherPrompt.SetActive(true);
            //StartRotatePrompt.SetActive(true);
        }
        else
        {
            //StartTetherPrompt.SetActive(false);
            //StartRotatePrompt.SetActive(false);
        }


        //Rotation Mode
        if (player.GetComponent<LassoTetherController>().CurrentLassoState == LassoState.SnapRotating)
        {
            //RotateControlsPrompt.SetActive(true);
            //ReturnPrompt.SetActive(true);
        }
        else
        {
           // RotateControlsPrompt.SetActive(false);
            //ReturnPrompt.SetActive(false);
        }





        /*
        print(Outline.isOutlined);

        if (Outline.isOutlined)
        {
           GrabPrompt.SetActive(true);
        }
        else
        {
            GrabPrompt.SetActive(false);
        }
        */



    }
}
