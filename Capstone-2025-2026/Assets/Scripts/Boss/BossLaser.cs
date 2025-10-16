using System.Xml.Serialization;
using UnityEngine;

public class BossLaser : MonoBehaviour
{
    [SerializeField] private float laserLength = 20.0f;
    [SerializeField] private float laserRadius = 1.2f;

    [SerializeField] Transform[] laserHitChecks;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        foreach(Transform t in laserHitChecks)
        {
            Ray ray = new Ray(t.position, t.forward);
            if (Physics.Raycast(ray,out RaycastHit hit, laserLength))
            {
                if(hit.collider.attachedRigidbody.GetComponent<BossArmorWall>() != null)
                {
                    float sourceToHitDist = Vector3.Distance(transform.position, hit.point);
                    transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, sourceToHitDist / 2);
                    return;
                }
            }
        }

        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, laserLength / 2);
    }

    private void OnTriggerEnter(Collider other)
    {
        other.attachedRigidbody.AddForce(transform.forward * 30f + new Vector3(0,20f,0), ForceMode.Impulse);
    }
}
