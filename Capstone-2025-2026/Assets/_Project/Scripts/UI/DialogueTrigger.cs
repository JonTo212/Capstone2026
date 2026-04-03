using FMODUnity;
using StarterAssets;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class DialogueTrigger : MonoBehaviour
{

    public bool repeatable;
    private bool triggered = false;
    
    [SerializeField] private string desiredText;
    [SerializeField] private SpeakerType speakerOptionsDropdown;
    [SerializeField] private float characterSpeed = 5f;
    [SerializeField] private float punctuationSpeed = 0.5f;
    
    [SerializeField] private float delay = 0;

    [SerializeField] private GameObject template;
    [SerializeField] private GameObject NewDialogueStorage;
    private void Start()
    {
        NewDialogueStorage = GameObject.Find("NewDialogueStorage");
        template = GameObject.Find("NPCDialogue 1");
    }
   
    
    private void OnTriggerEnter(Collider other)
    {
        if (triggered == false || repeatable == true)
        {
            triggered = true;
            if (other.gameObject.CompareTag("Player"))
            {
                Invoke(nameof(CreateNPCDialogue), delay);
            }
        }
    }

    public void CreateNPCDialogue()
    {

        NPCDialogue npcText;
        GameObject newBox = Instantiate(template, template.transform.position , Quaternion.identity, NewDialogueStorage.transform);
        Image[] images = newBox.GetComponentsInChildren<Image>();
        foreach (Image image in images) { image.enabled = true; }
        TMP_Text text = newBox.GetComponentInChildren<TMP_Text>();
        text.enabled = true;
        npcText = newBox.GetComponentInChildren<NPCDialogue>();
        npcText.charactersPerSecond = characterSpeed;
        npcText.interpunctuationDelay = punctuationSpeed;
        npcText.SetText(desiredText, speakerOptionsDropdown);
    }
}
