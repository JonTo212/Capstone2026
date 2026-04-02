using UnityEngine;

public class dialogDebuggingScript : MonoBehaviour
{
    [SerializeField] bool startDialog = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (startDialog)
        {
            
            GetComponent<DialogueTrigger>().CreateNPCDialogue();
            startDialog = false;
        }
    }
}
