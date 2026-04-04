using System;
using UnityEngine;

public class RodAnimations : MonoBehaviour
{
    [Header("Rod Animations")]
    [SerializeField] private Animator animator;
    [SerializeField] private LassoTetherController lassoTetherControllerScript;


    [Header("Rotate Handle")]
    public static RodAnimations Instance;
    [SerializeField] private PlayerActions playerActions;
    [SerializeField] private GameObject handle;
    public float rotationAmount = 5f;

    [Header("Core Material")]
    [SerializeField] private Material toolMat;

    [Header("SFX (STILL NEEDS UPDATE TO JUAN NEW SYSTE)")]
    [SerializeField] private AudioSource audioSource; // The AudioSource component
    [SerializeField] private AudioClip[] clips;

    [Header("ITS FMOD YOU SHOULD KNOW HOW TO ADD THE SOUND")]
    [SerializeField] public bool facts = true;


    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        //Transformations
        if (lassoTetherControllerScript.rodEquipped)
        {
            animator.SetBool("isRod", true);
            animator.SetBool("isTether", false);

            toolMat.SetColor("_MainColor", Color.yellow * 8);
        }
        else
        {
            animator.SetBool("isRod", false);
            animator.SetBool("isTether", true);

            toolMat.SetColor("_MainColor", Color.cyan * 8);
        }



        float scrollValue = playerActions.GetDPadScrollValue();

        if (scrollValue != 0)
        {
            Debug.Log("Scroll value: " + scrollValue);
            
            handle.transform.Rotate(Vector3.left, scrollValue * rotationAmount);
            //PlayRandomSound();
        }

    }

    public void PlayRandomSound()
    {
        //prevents game from crashing if i forgot to add clips
        if (clips.Length == 0) return;

        // Pick a random clip
        //int randomIndex = Random.Range(0, clips.Length);

        // Play the clip as a one-shot
        //audioSource.PlayOneShot(clips[randomIndex]);

    }
}
