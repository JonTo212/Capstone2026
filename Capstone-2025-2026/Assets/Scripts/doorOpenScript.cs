using UnityEngine;

public class doorOpenScript : MonoBehaviour
{
    public Animator animator;

    public GameObject handle;
    private Tetherable tetherableScript;
    private Rigidbody rb;

    public bool primed = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        tetherableScript = handle.GetComponent<Tetherable>();
        rb = handle.GetComponent<Rigidbody>();



    }

    // Update is called once per frame
    void Update()
    {
        if (tetherableScript.isHeld)
        {
            primed = true;
        }

        if ((rb.useGravity == true) && (primed == true))
        {
            // play animation
            animator.Play("swingOpen");
            primed = false;
            print("open");
        }


    }
}
