using UnityEngine;

public class puffballPoof : MonoBehaviour
{
    public ParticleSystem particle;

    public GameObject[] puffballs;



    private void OnTriggerEnter(Collider other)
    {
        foreach (GameObject puffball in puffballs)
        {
            Destroy(puffball);
        }

        //instantiate particle
        particle.Play();
    }
}
