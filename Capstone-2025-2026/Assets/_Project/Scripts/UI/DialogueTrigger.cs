using StarterAssets;
using Unity.VisualScripting;
using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{

    public bool repeatable;
    private bool triggered = false;
    
    [SerializeField] private string desiredText;
    [SerializeField] private SpeakerType speakerOptionsDropdown;
    [SerializeField] private float characterSpeed = 5f;
    [SerializeField] private float punctuationSpeed = 0.5f;
    
    [SerializeField] private float delay = 0;

    public GameObject template;
    public GameObject uICanvas;
    private void Start()
    {
        uICanvas = GameObject.Find("PlayerUICanvas");
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

    private void CreateNPCDialogue()
    {
        NPCDialogue npcText;
        GameObject newBox = Instantiate(template, template.transform.position + new Vector3(834,26,0), Quaternion.identity, uICanvas.transform);
        npcText = newBox.GetComponentInChildren<NPCDialogue>();
        npcText.charactersPerSecond = characterSpeed;
        npcText.interpunctuationDelay = punctuationSpeed;
        npcText.SetText(desiredText, speakerOptionsDropdown);
    }
}
