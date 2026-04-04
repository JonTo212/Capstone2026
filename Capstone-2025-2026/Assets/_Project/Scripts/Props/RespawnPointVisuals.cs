
using UnityEngine;


public class RespawnPointVisuals : MonoBehaviour
{
    public bool activated = false;
    public bool firstTime = true;
    public Vector3 Forward;

    //public GameObject activeFlag;
    //public GameObject inactiveFlag;
    //[SerializeField] private ParticleSystem particles;

    private void Awake()
    {
        Forward = transform.forward;
    }

    public void SetObjectActive()
    {
        //gameObject.transform.DOPunchScale(new Vector3(1.1f,1.1f,1.1f), 2f, 0);
        if (!activated)
        {
            activated = true;

            //activeFlag.SetActive(true);
            //inactiveFlag.SetActive(false);

            //particles.gameObject.SetActive(true);
            //particles.Play();
}
        else
        {
            activated = false;

            //activeFlag.SetActive(false);
            //inactiveFlag.SetActive(true);
        }
    }

    //public void PlayFanfare()
    //{
    //    AudioManager.Instance.PlaySFX(AudioManager.Instance.FlagFare,9,5f);
    //    firstTime = false;
    //    //Audio here too IG
    //}
}
