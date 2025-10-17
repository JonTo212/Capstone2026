using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;
using static Unity.Cinemachine.CinemachineTargetGroup;
using static UnityEngine.GraphicsBuffer;

public class CircleOrbit : MonoBehaviour
{
    [Header("Components")]
    public GameObject [] debris;
    private SphereCollider sphereCollider;


    [Header ("Orbit Properties")]
    [SerializeField] private float maxHeight;
    public float minSpawnRadius;
    public float maxSpawnRadius;
    public float spawnAmount;
    private float radius;
    private float colliderBuffer; // should shrink the collider a a bit so debris doesnt get too far away

    [Header("Debris Properties")]
    public float baseOrbitSpeed;
    public float orbitSpeedVariation;
    public float slowSpd;
    private float orbitSpeed;
    private Vector3 randomCircle;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SpawnDebris();

        //random orbit speed
        orbitSpeed = Random.Range(baseOrbitSpeed - orbitSpeedVariation, baseOrbitSpeed + orbitSpeedVariation);

        //get sphere collider
        sphereCollider = GetComponent<SphereCollider>();
        sphereCollider.radius = maxSpawnRadius;

    }

    private void FixedUpdate()
    {
        UpdateDebrisInOrbit();
    }

    void SpawnDebris() //https://discussions.unity.com/t/finding-a-circumference-point/35284
    {
        for (int i = 0; i < spawnAmount; i++)
        {
            radius = Random.Range(minSpawnRadius, maxSpawnRadius);
            float angle = Random.Range(0, 360);

            randomCircle = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad));

            GameObject newDebris = debris[Random.Range(0, debris.Length)];
            Instantiate(newDebris, transform.position+randomCircle*radius, Quaternion.identity, this.transform);
        }
    }

    void UpdateDebrisInOrbit()
    {
        foreach (Transform child in transform)
        {
            // Spin children around parent
            child.transform.RotateAround(new Vector3(transform.position.x, transform.position.y, transform.position.z), Vector3.up, orbitSpeed * Time.deltaTime);

            //unparent if held
            if (child.GetComponent<Prop>().IsHeld)
            {
                child.transform.parent = null;
                //print("ORPHAN");
            }
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        if (other.GetComponent<Prop>())
        {
            if (!other.GetComponent<Rigidbody>().isKinematic)
            {
                if (!other.GetComponent<Prop>().IsHeld)
                {
                    //get distance between self and target
                    Vector2 orbitPos = new Vector2(transform.position.x, transform.position.z);
                    Vector2 debrisPos = new Vector2(other.transform.position.x, other.transform.position.z);

                    float orbitToDebrisDist = Vector2.Distance(orbitPos, debrisPos);
                    float orbitToDebrisHeigtDiff = Mathf.Abs(transform.position.y - other.transform.position.y);

                    //check distance
                    if ((orbitToDebrisDist < maxSpawnRadius) && (orbitToDebrisDist > minSpawnRadius) && (orbitToDebrisHeigtDiff < maxHeight))// && (orbitToDebrisHeigtDiff < maxHeight)) /*&& target.GetComponent<Rigidbody>().linearVelocity.magnitude > 10f*/
                    {
                        //set parent
                        other.transform.parent = this.transform;

                        //turn off gravity
                        other.GetComponent<Rigidbody>().useGravity = false;

                        //smoothly stop all inertia/movement when reparented
                        //StartCoroutine(SlowDown(other.gameObject));

                        //other.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;


                        Rigidbody rb = other.GetComponent<Rigidbody>();

                        if (rb.linearVelocity.magnitude > 0.05f)
                        {
                            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.deltaTime * slowSpd);
                        }
                        else
                        {
                            rb.linearVelocity = Vector3.zero;
                        }

                    }
                    else
                    {

                        other.GetComponent<Rigidbody>().useGravity = true;

                        other.transform.parent = null;
                    }
                }
            }
        }
    }
}
