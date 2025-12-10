using Microsoft.Unity.VisualStudio.Editor;
using Unity.VisualScripting;
using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{

    public bool repeatable;
    
    [SerializeField]private string desiredText;
    [SerializeField] private SpeakerType speakerOptionsDropdown;


    //public Image[] profiles;
   
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

        if (other.gameObject.CompareTag("Player"))
        {
            npcText = other.GetComponentInChildren<NPCDialogue>();
            npcText.SetText(desiredText, speakerOptionsDropdown);
        }
    }
}
