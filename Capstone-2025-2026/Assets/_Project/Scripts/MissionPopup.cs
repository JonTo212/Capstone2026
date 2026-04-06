using DG.Tweening;
using FMODUnity;
using UnityEngine;

public class MissionPopup : MonoBehaviour
{

    public GameObject missionText;
    private DOTweenAnimation missionTextdotweenAnimator;

    public GameObject sparkle;
    private DOTweenAnimation sparkleDoTweenAnimator;

    private Collider col;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        missionTextdotweenAnimator = missionText.GetComponent<DOTweenAnimation>();
        sparkleDoTweenAnimator = sparkle.GetComponent<DOTweenAnimation>();

        col = GetComponent<Collider>();
    }


    private void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Player"))
        {
            missionTextdotweenAnimator.DOPlay();
            sparkleDoTweenAnimator.DOPlay();

            col.enabled = false;
        }

    }

}
