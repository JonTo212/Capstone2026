using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MusicUITrigger : MonoBehaviour
{
    GameObject musicUI;
    TMP_Text uiText;
    Image uiOutline;

    public float duration = 2;
    public string names;

    void Start()
    {
        musicUI = GameObject.Find("MusicUI");  
        uiText = musicUI.GetComponentInChildren<TMP_Text>();
        uiOutline = musicUI.GetComponentInChildren<Image>(); 
    }

    void OnTriggerEnter(Collider other)
    {
        string text = "♫ Now Playing ♫\n\n" + names;
        uiText.SetText(text);
        uiText.DOFade(1f,duration);
        uiOutline.DOFade(1f,duration);
        Invoke(nameof(FadeOut), duration*2);
    }

    private void FadeOut()
    {
        uiText.DOFade(0f, duration);
        uiOutline.DOFade(0f, duration);
    }
}
