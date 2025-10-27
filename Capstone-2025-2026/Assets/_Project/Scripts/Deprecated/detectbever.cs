using UnityEngine;

public class detectbever : MonoBehaviour
{
    public Animator animator;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("animal"))
        {
            //play animation
            animator.Play("bounceanim");

            //destroy what triggered the collider
            Destroy(other.gameObject);
        }
    }
}
