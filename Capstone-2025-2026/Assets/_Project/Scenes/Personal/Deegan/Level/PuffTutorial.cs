using FMODUnity;
using UnityEngine;

public class PuffTutorial : PluckOutProp
{
    //[SerializeField] Transform helpText;
    //[SerializeField] Transform savedText;
    [SerializeField] Transform savedFishTransform;
    [SerializeField] Transform platform;
    [SerializeField] Transform[] debrisToDestroy;

    public CritterInstance CritterInstanceScript;
    [SerializeField] private GameObject grabIndicator;

    private void Awake()
    {
        Init();
    }

    public override void ActivateOutline(bool activate)
    {
        base.ActivateOutline(activate);
        grabIndicator.SetActive(activate);
    }

    protected override void OnPluck()
    {
        base.OnPluck();

        //savedText.gameObject.SetActive(true);
        //helpText.gameObject.SetActive(false);

        CritterInstanceScript.BeRescued(); // this tells eloras UI to add guy as saved

        RuntimeManager.PlayOneShot("event:/Pluck", transform.position);
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
