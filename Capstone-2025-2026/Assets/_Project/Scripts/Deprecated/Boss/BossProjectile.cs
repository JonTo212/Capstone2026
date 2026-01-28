using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    Rigidbody rb;
    Transform player;
    float speedMultiplier = 2f;
    float projectileStrength = 20f;
    bool grounded = false;
    public GameObject bossRef;
    public float projectileLifetime = 15f;
    public AudioManager aManage;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        aManage = GameObject.Find("AudioManager").GetComponent<AudioManager>();
        rb = GetComponent<Rigidbody>();
        player = GameObject.Find("Player").transform;

        Invoke(nameof(Seek), 5f);
        Invoke(nameof(DestroyProjectileAfterAWhile), projectileLifetime);
    }

    // Update is called once per frame
    void Update()
    {
        if (!grounded)
        {
            transform.Rotate(new Vector3(1, 1, 1) * speedMultiplier);
        }
        else
        {
            if (GetComponent<Prop>().IsTetherPulled || GetComponent<Prop>().IsSnared)
            {
                rb.isKinematic = false;
            }
        }
    }

    public void Seek()
    {
        Vector3 SetPosition = player.transform.position;
        rb.AddForce((SetPosition - transform.position) * 0.5f, ForceMode.Impulse);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == 6 && !grounded)
        {
            aManage.PlaySFX(aManage.WallBreak, 3, 1f);
            transform.parent = null;
            rb.isKinematic = true;
            grounded = true;
        }
        else if(!grounded)
        {
            if (collision.gameObject.TryGetComponent(out PlayerController pc))
            {
                aManage.PlaySFX(aManage.PlayerBadlyHurt, 6, 1f);
            }
            if(collision.gameObject.TryGetComponent(out Rigidbody rb))
            { 
                rb.AddForce((bossRef.transform.forward + bossRef.transform.up) * projectileStrength, ForceMode.Impulse);
            }
            DestroyProjectileAfterAWhile();
        }
    }

    private void DestroyProjectileAfterAWhile()
    {
        aManage.PlaySFX(aManage.WallBreak, 3, 1f);
        Destroy(gameObject);
    }
}
