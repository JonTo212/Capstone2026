using System;
using System.Collections;
using UnityEngine;
using TMPro;
using System.Globalization;
using Microsoft.Unity.VisualStudio.Editor;
using System.Collections.Generic;

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

    [Header("Typewriter Settings")]
    [SerializeField] private float charactersPerSecond = 20;
    [SerializeField] private float interpunctuationDelay = 0.5f;

    [Header("Profile Settings")]
    public Image[] profiles;
    public Image currentSpeaker;

    private Dictionary<SpeakerType, Image> speakerImageDictionary = new Dictionary<SpeakerType, Image>();


    //ANIMATE THIS WITH DOTWEEN
    private void Awake()
    {
        
        _textBox = GetComponent<TMP_Text>();
        _simpleDelay = new WaitForSeconds(1/charactersPerSecond);
        _interpunctuationDelay = new WaitForSeconds(interpunctuationDelay);
        speakerImageDictionary[SpeakerType.Player] = profiles[0];
        speakerImageDictionary[SpeakerType.Player] = profiles[1];
        
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetText(testText, 0);
    }

    public void SetText(string text, SpeakerType speakerType)
    {
        currentSpeaker = speakerImageDictionary[speakerType];

        if(_typewriterCoroutine != null)
        {
            StopCoroutine(_typewriterCoroutine);
        }
        _textBox.text = text;
        _textBox.maxVisibleCharacters = 0;
        _currentVisibleCharacterIndex = 0;

        _typewriterCoroutine = StartCoroutine(routine:Typewriter());
    }

    private IEnumerator Typewriter()
    {
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

    // Update is called once per frame
    void Update()
    {
        
    }
}
