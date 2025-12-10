using NodeCanvas.Tasks.Actions;
using UnityEngine;

public class RespawnPointVisuals : MonoBehaviour
{
    public bool activated = false;
    public GameObject activeFlag;
    public GameObject inactiveFlag;
    [SerializeField] private ParticleSystem particles;

    public void SetObjectActive()
    {
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
}
