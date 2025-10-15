using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    Rigidbody rb;
    Transform player;
    float speedMultiplier = 2f;
    float projectileStrength = 2f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        player = GameObject.Find("Player").transform;

        Invoke(nameof(Seek), 5f);
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(new Vector3(1,1,1)* speedMultiplier);
    }

    public void Seek()
    {
        Vector3 SetPosition = player.transform.position;
        rb.AddForce((SetPosition - transform.position) * 0.5f, ForceMode.Impulse);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent(out PlayerController controller))
        {
            collision.gameObject.GetComponent<Rigidbody>().AddForce((-collision.gameObject.transform.forward + collision.gameObject.transform.up) * projectileStrength);
        }
        else if(collision.gameObject.layer == 6)
        {
            rb.isKinematic = true;
        }
    }
}
