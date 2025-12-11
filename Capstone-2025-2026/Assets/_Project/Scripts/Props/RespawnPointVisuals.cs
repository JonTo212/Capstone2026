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
        particles.Play();
        firstTime = false;
        //Audio here too IG
    }
}
