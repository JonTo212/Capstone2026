using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(Image))]
public class ImageFader : MonoBehaviour
{
    private Image image;
    private Tween currentTween;

    [Header("Settings")]
    public float duration = 0.8f;

    public bool FadeComplete { get; private set; }

    void Awake()
    {
        image = GetComponent<Image>();
        image.color = new Color(image.color.r, image.color.g, image.color.b, 0);
    }

    public void FadeIn()
    {
        FadeComplete = false;
        currentTween?.Kill();
        currentTween = image.DOFade(1f, duration);
        currentTween.OnComplete(() => FadeComplete = true);
    }

    public void FadeOut(float? customDuration = null)
    {
        FadeComplete = false;
        currentTween?.Kill();
        currentTween = image.DOFade(0f, duration);
        currentTween.OnComplete(() => FadeComplete = true);
    }

    public void SetImageAlpha(float alpha)
    {
        image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
    }
}