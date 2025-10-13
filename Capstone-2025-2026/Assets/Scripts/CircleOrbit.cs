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
    public GameObject debris;
    private Tetherable tetherableScript;
    private SphereCollider sphereCollider;




    [Header ("Orbit Properties")]
    private float radius;
    private float colliderBuffer; // should shrink the collider a a bit so debris doesnt get too far away
    public float minSpawnRadius;
    public float maxSpawnRadius;
    [SerializeField] private float maxHeight;
    public float spawnAmount;


    [Header("Debris Properties")]
    public float baseOrbitSpeed;
    private float orbitSpeed;
    public float orbitSpeedVariation;

    public float slowSpd;

    private Vector3 randomCircle;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DebrisSpawner();

        //random orbit speed
        orbitSpeed = Random.Range(baseOrbitSpeed - orbitSpeedVariation, baseOrbitSpeed + orbitSpeedVariation);

        //get sphere collider
        sphereCollider = GetComponent<SphereCollider>();
        sphereCollider.radius = maxSpawnRadius;

    }

    private void FixedUpdate()
    {
        DebrisOrbitLogic();
    }

    void DebrisSpawner() //https://discussions.unity.com/t/finding-a-circumference-point/35284
    {
        
        for (int i = 0; i < spawnAmount; i++)
        {
            radius = Random.Range(minSpawnRadius, maxSpawnRadius);
            float angle = Random.Range(0, 360);

            randomCircle = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad));

            Instantiate(debris, transform.position+randomCircle*radius, Quaternion.identity, this.transform);
        }
    }

    void DebrisOrbitLogic()
    {
        foreach (Transform child in transform)
        {
            // Spin children around parent
            child.transform.RotateAround(new Vector3(transform.position.x, transform.position.y, transform.position.z), Vector3.up, orbitSpeed * Time.deltaTime);

            //get tetherscript from each child
            tetherableScript = child.GetComponent<Tetherable>();

            //unparent if held
            if (tetherableScript.isHeld)
            {
                child.transform.parent = null;
                print("ORPHAN");
            }
        }

    }

    
    private void OnTriggerStay(Collider other)
    {
        if (other.GetComponent<Tetherable>())
        {
            if (!other.GetComponent<Tetherable>().isHeld)
            {
                //get distance between self and target
                var orbitPos = new Vector2(transform.position.x, transform.position.z);
                var debrisPos = new Vector2(other.transform.position.x, other.transform.position.z);

                var diff = Vector2.Distance(orbitPos, debrisPos);
                var heightDiff = Mathf.Abs(transform.position.y - other.transform.position.y);


                //check distance
                if ((diff < maxSpawnRadius) && (diff > minSpawnRadius) && (heightDiff < maxHeight))// && (heightDiff < maxHeight)) /*&& target.GetComponent<Rigidbody>().linearVelocity.magnitude > 10f*/
                {
                    //set parent
                    other.transform.parent = this.transform;

                    //turn off gravity
                    other.GetComponent<Rigidbody>().useGravity = false;

                    //smoothly stop all inertia/movement when reparented
                    //StartCoroutine(SlowDown(other.gameObject));

                    //other.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;

                    
                    var rb = other.GetComponent<Rigidbody>();

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
