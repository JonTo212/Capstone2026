using UnityEngine;

public class puffballPoof : MonoBehaviour
{
    public ParticleSystem particle;

    public GameObject[] puffballs;



    private void OnTriggerEnter(Collider other)
    {
        //instantiate particle
        particle.Play();

        foreach (GameObject puffball in puffballs)
        {
            Destroy(puffball);
        }
    }
}
