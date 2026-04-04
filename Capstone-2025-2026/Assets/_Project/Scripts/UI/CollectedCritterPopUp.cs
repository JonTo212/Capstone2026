using UnityEngine;
using TMPro;
using DG.Tweening;

public class CollectedCritterPopUp : MonoBehaviour
{
    private Transform popUp1;
    private Transform popUp2;

    private DOTweenAnimation popUp1Anim;
    private DOTweenAnimation popUp2Anim;

    private void Awake()
    {

        popUp1 = this.gameObject.transform.GetChild(0);
        popUp2 = this.gameObject.transform.GetChild(1);


        popUp1.gameObject.SetActive(false);
        popUp2.gameObject.SetActive(false);

        popUp1Anim = popUp1.GetComponent<DOTweenAnimation>();
        popUp2Anim = popUp2.GetComponent<DOTweenAnimation>();

    }

    public void RunAnims ()
    {
        //Collected critter sound fx
        Debug.Log("Juan put Sound FX here.");

        popUp1Anim.DORestart();
        popUp1.gameObject.SetActive(true);
        popUp1Anim.DOPlay();

        popUp2Anim.DORestart();
        popUp2.gameObject.SetActive(true);
        popUp2Anim.DOPlay();

    }

}
