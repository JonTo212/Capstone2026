using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class BlackPromptBGFade : MonoBehaviour
{
    private Image image;
    private float fadeDuration = .8f;    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        image = GetComponent<Image>();

        image.color = new Color(0, 0, 0, 0);
    }

    public void FadeIn()
    {
        image.DOFade(1, fadeDuration);
    }

    public void FadeOut()
    {
        image.DOFade(0, fadeDuration);
    }
}
