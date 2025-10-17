using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;

public class DivingBell : MonoBehaviour
{
    public Animator animator;
    public int bellHP = 2;

    //sound 
    private AudioSource audioSource;
    public AudioClip ringSound;
    public AudioClip breakSound;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        if (bellHP <= 0 && !AnimatorIsPlaying())
        {
            //Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hammer"))
        {
            bellHit();
            Destroy(other.gameObject);
        }      
    }

    void bellHit()
    {
        bellHP--;

        if (bellHP > 0)
        {
            animator.Play("bellRingAnim", 0, 0f);
            
        }

        if (bellHP == 0)
        {
            animator.Play("bellFallAnim", 0, 0f);
        }

        
        if (!audioSource.isPlaying)
        {
            if (bellHP >-1)
            {
                audioSource.PlayOneShot(ringSound, 1);
                audioSource.PlayOneShot(breakSound, .25f);
            }
            else
            {
                audioSource.PlayOneShot(breakSound, 1);
            }

        }

  

    }

    bool AnimatorIsPlaying() //https://discussions.unity.com/t/how-can-i-check-if-an-animation-is-playing-or-has-finished-using-animator-c/57888/4
    {
        //return animator.GetCurrentAnimatorStateInfo(0).length > animator.GetCurrentAnimatorStateInfo(0).normalizedTime;

        return animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1;
    }


}
