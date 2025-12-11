using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

public class ScreenTransitions : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void Start()
    {
        Tween fadeOut = gameObject.GetComponent<Image>().DOFade(0, 2f);
        fadeOut.onComplete += () => gameObject.SetActive(false);
        Sequence startFadeIn = DOTween.Sequence(fadeOut);
        startFadeIn.Play();
    }

    public void FadeToBlack()
    {
        //Doesn't work
        /*gameObject.SetActive(true);
        gameObject.GetComponent<Image>().color = Color.black;
        gameObject.GetComponent<Image>().DOFade(0, 0.001f);
        gameObject.GetComponent<Image>().DOFade(255, 2f);*/
    }
}
