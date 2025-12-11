using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Globalization;
using System.Collections.Generic;
using DG.Tweening;

public enum SpeakerType
    {
        Player,
        Momma,

    }

public class NPCDialogue : MonoBehaviour
{

    private TMP_Text _textBox;

    [Header("Test String")]
    [SerializeField] private string testText;

    private int _currentVisibleCharacterIndex;
    private Coroutine _typewriterCoroutine;

    private WaitForSeconds _simpleDelay;
    private WaitForSeconds _interpunctuationDelay;
    public Vector3 originalDialoguePosition;
    public Vector3 hiddenPositionOffset;
    public Vector3 hiddenPosition;
    public GameObject dialogueBox;

    [Header("Typewriter Settings")]
    [SerializeField] private float charactersPerSecond = 3;
    [SerializeField] private float interpunctuationDelay = 0.5f;

    [Header("Profile Settings")]
    public Image currentSpeaker;
    [SerializeField] private Sprite[] profiles;

    private Dictionary<SpeakerType, Sprite> speakerImageDictionary = new Dictionary<SpeakerType, Sprite>();


    //ANIMATE THIS WITH DOTWEEN
    private void Awake()
    {
        _textBox = GetComponent<TMP_Text>();
        _simpleDelay = new WaitForSeconds(1/charactersPerSecond);
        _interpunctuationDelay = new WaitForSeconds(interpunctuationDelay);
        speakerImageDictionary[SpeakerType.Player] = profiles[0];
        speakerImageDictionary[SpeakerType.Momma] = profiles[1];

        originalDialoguePosition = dialogueBox.GetComponent<RectTransform>().anchoredPosition;
        hiddenPosition = originalDialoguePosition + hiddenPositionOffset;

    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
        dialogueBox.GetComponent<RectTransform>().anchoredPosition = hiddenPosition;
        //SetText(testText, 0);
    }

    public void SetText(string text, SpeakerType speakerType)
    {
        currentSpeaker.sprite = speakerImageDictionary[speakerType];

        if(_typewriterCoroutine != null)
        {
            StopCoroutine(_typewriterCoroutine);
        }
        _textBox.text = text;
        _textBox.maxVisibleCharacters = 0;
        _currentVisibleCharacterIndex = 0;

        _typewriterCoroutine = StartCoroutine(Typewriter());
    }

    private IEnumerator Typewriter()
    {
        TextOnScreen();
        TMP_TextInfo textInfo = _textBox.textInfo;
        while (_currentVisibleCharacterIndex < textInfo.characterCount + 1)
        {
            char character = textInfo.characterInfo[_currentVisibleCharacterIndex].character;
            _textBox.maxVisibleCharacters++;

            if((character == '?' | character == '.' || character ==',' ||character == ':' ||character ==';' ||character =='!' || character == '-'))
            {
                yield return _interpunctuationDelay;
            }
            else
            {
                yield return _simpleDelay;
            }

            _currentVisibleCharacterIndex++;
        }
    }

    public void TextOnScreen()
    {
        dialogueBox.GetComponent<RectTransform>().DOAnchorPos(originalDialoguePosition, 1);//DOMove(originalDialoguePosition, 2);
        Invoke(nameof(TextOffScreen), 6);
    }



    public void TextOffScreen()
    {
        dialogueBox.GetComponent<RectTransform>().DOAnchorPos(hiddenPosition, 1); // DOMove(hiddenPosition, 2);
    }
}
