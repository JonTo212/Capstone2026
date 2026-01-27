using System.Collections;
using UnityEngine;

public class StuckOnABranch : MonoBehaviour
{
    private Rigidbody rb;
    private float timeElapsed = 0f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        StartCoroutine(UnstuckAfterDelay());
    }

    IEnumerator UnstuckAfterDelay()
    {
        yield return new WaitForSeconds(4f);

        rb.isKinematic=false;
    }
}
