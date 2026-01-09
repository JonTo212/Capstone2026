using UnityEngine;

public class BossShield : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.rigidbody.GetComponent<Prop>() != null)
        {
            Destroy(collision.gameObject);
        }
    }
}
