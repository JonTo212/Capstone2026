using UnityEngine;

public class PuffTutorial : PluckOutProp
{
    [SerializeField] Transform helpText;
    [SerializeField] Transform savedText;
    [SerializeField] Transform savedFishTransform;
    [SerializeField] Transform platform;
    [SerializeField] Transform[] debrisToDestroy;

    private void Awake()
    {
        Init();
    }

    protected override void OnPluck()
    {
        base.OnPluck();

        savedText.gameObject.SetActive(true);
        helpText.gameObject.SetActive(false);
        savedFishTransform.gameObject.SetActive(true);
        platform.gameObject.SetActive(true);
        GetComponent<DialogueTrigger>().CreateNPCDialogue();
        foreach(Transform t in debrisToDestroy)
        {
            Destroy(t.gameObject);
        }
        Destroy(gameObject);
    }
}
