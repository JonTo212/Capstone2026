using DG.Tweening;
using FMOD;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SimpleTutorialTextManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public PlayerActions playerActions;
    public bool tutorialCleared = false;
    public GameObject tutorialText;
    public Image[] controllerPrompts;
    public bool repeatable = false;
    public bool active = false;

    void Start()
    {
        playerActions = FindAnyObjectByType<PlayerActions>().GetComponent<PlayerActions>();
        Fade(0, 0.0001f);
    }

    private void Update()
    {
        if (playerActions.JumpDown)
        {
            Fade(0, 2);
            if (!repeatable && active) Destroy(this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            active = true;
            Fade(1, 2);
        }
    }

    public void Fade(int amount, float duration)
    {
        tutorialText.GetComponent<TMP_Text>().DOFade(amount, duration);
        controllerPrompts = tutorialText.GetComponentsInChildren<Image>();
        foreach (Image image in controllerPrompts)
        {
            image.DOFade(amount, duration);
        }
    }
}
