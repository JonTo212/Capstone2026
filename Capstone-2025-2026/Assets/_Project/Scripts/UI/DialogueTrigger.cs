using Unity.VisualScripting;
using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{

    public bool repeatable;
    private bool triggered = false;
    
    [SerializeField] private string desiredText;
    [SerializeField] private SpeakerType speakerOptionsDropdown;
   
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter(Collider other)
    {
        NPCDialogue npcText;
        if (triggered == false || repeatable == true)
        {
            triggered = true;
            if (other.gameObject.CompareTag("Player"))
            {
                npcText = other.GetComponentInChildren<NPCDialogue>();
                npcText.SetText(desiredText, speakerOptionsDropdown);
            }
        }
    }
}
