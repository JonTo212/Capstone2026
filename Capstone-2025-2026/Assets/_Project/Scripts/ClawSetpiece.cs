using System.Collections;
using UnityEngine;

public class ClawSetpiece : MonoBehaviour
{
    private ClawHead clawHeadScript;
    public TetherVessel vesselScript;

    private bool startBreak;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        clawHeadScript = GetComponent<ClawHead>();
    }

    // Update is called once per frame
    void Update()
    {
        if (clawHeadScript.currentSelectedProp != null)
        {
            if (startBreak == false)
            {
                StartCoroutine(Break());
                startBreak = true;
            }   
        }
    }

    IEnumerator Break()
    {
        //play sound
        AudioManager.Instance.PlaySFX(AudioManager.Instance.CrackingVessel, 1, 1.2f);
        yield return new WaitForSeconds(3f);


        //play break animation
        AudioManager.Instance.PlaySFX(AudioManager.Instance.GrabberExplode, 1, .8f);
        vesselScript.exposeDoor = true;
        Destroy(gameObject);
    }
}
