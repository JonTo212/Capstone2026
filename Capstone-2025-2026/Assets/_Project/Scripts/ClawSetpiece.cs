using FMODUnity;
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

        clawHeadScript.OnAttachToObject += AttachClawToVessel;
        clawHeadScript.OnDetachToObject += DetachClawToVessel;
    }

    // Update is called once per frame
    void Update()
    {
        if (clawHeadScript.currentSelectedProp != null)
        {
            if (startBreak == false)
            {

                RuntimeManager.PlayOneShot("event:/MenuSelect", transform.position);
                //AudioManager.Instance.PlaySFX(AudioManager.Instance.CrackingVessel, 1, 1.2f);
                startBreak = true;
            }   
        }
    }

    IEnumerator Break()
    {
        //play sound
        RuntimeManager.PlayOneShot("event:/MenuSelect", transform.position);
        //AudioManager.Instance.PlaySFX(AudioManager.Instance.CrackingVessel, 1, 1.2f);
        yield return new WaitForSeconds(3f);

    }

    private void AttachClawToVessel()
    {
        vesselScript.IncreaseAttachedClawCount(this);
        clawHeadScript.DisableInteraction();
    }

    private void DetachClawToVessel()
    {
        vesselScript.DecreaseAttachedClawCount(this);
    }
}
