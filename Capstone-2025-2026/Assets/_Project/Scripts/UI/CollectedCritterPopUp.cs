using UnityEngine;
using TMPro;
using DG.Tweening;

public class CollectedCritterPopUp : MonoBehaviour
{
    private Transform text_PopUp;
    private DOTweenAnimation text_Anim;

    private void Awake()
    {

        text_PopUp = this.gameObject.transform.GetChild(0);
        text_Anim = text_PopUp.GetComponent<DOTweenAnimation>();

    }

    public void RunAnims ()
    {
        //Collected critter sound fx
        Debug.Log("Juan put Sound FX here.");
        
        text_Anim.DORestart();
       
        text_PopUp.gameObject.SetActive(true);
        text_Anim.DOPlay();
    }

}
