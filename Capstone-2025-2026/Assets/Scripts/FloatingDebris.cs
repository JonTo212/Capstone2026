using UnityEngine;

public class FloatingDebris : MonoBehaviour
{
    private Rigidbody rb;
    public GameObject orbitSphere;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (rb != null)
        {
            if (rb.useGravity == true)
            {
                rb.isKinematic = false;
                transform.SetParent(null);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.name == "AntiOrbitSphere")
        {
            rb.useGravity = false;
            rb.isKinematic = true;
            transform.SetParent(orbitSphere.transform);

        }
    }
}
