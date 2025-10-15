using UnityEngine;

public class BossArmorWall : MonoBehaviour
{
    Rigidbody _rb;

    [SerializeField] float speedThresholdToStick = 10f;
    [SerializeField] Transform stickIndicator;
    [SerializeField] Transform boss;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        stickIndicator.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if ((GetComponent<Prop>().IsSnared))
        {

            transform.LookAt(boss.position);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            if(_rb.linearVelocity.magnitude > speedThresholdToStick)
            {
                Invoke(nameof(BecomeKinematic), 0.03f);
                stickIndicator.gameObject.SetActive(true);
            }
        }
    }

    private void BecomeKinematic()
    {
        _rb.isKinematic=true;
    }
}
