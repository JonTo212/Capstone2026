using UnityEngine;

public class PlantBounce : MonoBehaviour
{
    public Animator animator;
    public ParticleSystem particle;

    private void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {

            //play animation
            animator.Play("PlantBouncingAnimaion",0,0f);

            //instantiate particle
            if (particle !=null ) particle.Play();
            FMODUnity.RuntimeManager.PlayOneShot("event:/BushShake");


    }
}
