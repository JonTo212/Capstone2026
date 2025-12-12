using NodeCanvas.Tasks.Actions;
using UnityEngine;
using DG.Tweening;

public class RespawnPointVisuals : MonoBehaviour
{
    public bool activated = false;
    public bool firstTime = true;
    public GameObject activeFlag;
    public GameObject inactiveFlag;
    [SerializeField] private ParticleSystem particles;

    public void SetObjectActive()
    {
        gameObject.transform.DOPunchScale(new Vector3(1.1f,1.1f,1.1f), 2f, 0);
        if (!activated)
        {
            print("Activated");
            activated = true;

            activeFlag.SetActive(true);
            inactiveFlag.SetActive(false);

            particles.gameObject.SetActive(true);
            particles.Play();
}
        else
        {
            print("deactivated");
            activated = false;

            activeFlag.SetActive(false);
            inactiveFlag.SetActive(true);
        }
    }

    public void PlayFanfare()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.FlagFare,9,5f);
        firstTime = false;
        //Audio here too IG
    }
}
