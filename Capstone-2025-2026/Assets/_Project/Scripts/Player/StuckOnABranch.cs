using UnityEngine;

public class StuckOnABranch : MonoBehaviour
{
    private Rigidbody rb;
    private float timeElapsed = 0f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        timeElapsed += Time.deltaTime;

        if(timeElapsed > 4f)
        {
            rb.isKinematic=false;
        }
    }
}
