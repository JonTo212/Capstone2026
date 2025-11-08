using System.Collections.Generic;
using UnityEngine;

public class WindTunnel : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private ParticleSystem windParticles;
    [SerializeField] private BoxCollider boxCollider;

    [Header("Parameters")]
    [SerializeField] private float windStrength = 5f;
    [SerializeField] private float windSpeed = 5f;
    [SerializeField] private float damping = 2f;
    [SerializeField] private Vector3 tunnelSize = Vector3.one * 3;

    [Header("Random Props")]
    [SerializeField] bool preCook;
    [SerializeField] int spawnNumber = 20;
    [SerializeField] private GameObject[] propsToSpawn;

    private List<Prop> propsInWindTunnel = new List<Prop>();
    private Vector3 windDirection;

    [Header("Debug Parameters")]
    [SerializeField] private Mesh debugArrow;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        windDirection = transform.forward;
        boxCollider.center = new Vector3(0,0,tunnelSize.z / 2);
        boxCollider.size = tunnelSize;

        var shapeModule = windParticles.shape;
        shapeModule.scale = new Vector3(tunnelSize.x, tunnelSize.y, 0.1f);

        SpawnRandomAllObject();
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < tunnelSize.z / 6; i++)
        {
            Gizmos.color = new Color(Mathf.Abs(transform.forward.x), Mathf.Abs(transform.forward.y), Mathf.Abs(transform.forward.z)) ;
            Gizmos.DrawWireMesh(debugArrow, transform.position + i * transform.forward * 6, transform.rotation, Vector3.one * 0.5f);
        }
        DrawDebugShape.DrawCube(transform.position, transform.forward, transform.right, tunnelSize, Color.red, DrawDebugShape.DebugShapeDrawMode.Edge);

        var shapeModule = windParticles.shape;
        shapeModule.scale = new Vector3(tunnelSize.x, tunnelSize.y, 0.1f);

        var mainModule = windParticles.main;
        mainModule.startLifetime =  tunnelSize.z / windParticles.main.startSpeed.constant;
    }

    private void FixedUpdate()
    {
        foreach (Prop prop in propsInWindTunnel)
        {
            StabilizeRbSpeed(prop);

            if (prop.rb.linearVelocity.magnitude < windSpeed)
            {
                prop.ApplyForceInDirection(transform.forward, windSpeed, ForceMode.Force);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.GetComponent<Prop>() != null)
        {
            propsInWindTunnel.Add(other.GetComponent<Prop>());

            other.GetComponent<Rigidbody>().useGravity = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<Prop>() != null)
        {
            Prop prop = other.GetComponent<Prop>();
            //if(!prop.IsSnared || !prop.isTetherPulled)
            //{
            //    other.transform.position = transform.position + GetRandomStartPosition();
            //    return;
            //}
            propsInWindTunnel.Remove(other.GetComponent<Prop>());
            other.GetComponent<Rigidbody>().useGravity = true;
        }
    }
    private void StabilizeRbSpeed(Prop prop)
    {
        Vector3 currentSpeed = prop.rb.linearVelocity;

        Vector3 vectorToTargetSpeed = windDirection * windSpeed - currentSpeed;

        Vector3 directionToTargetSpeed = vectorToTargetSpeed.normalized;

        Vector3 force = directionToTargetSpeed * windStrength;

        prop.rb.AddForce(force, ForceMode.Force);
    }

    private void SpawnRandomAllObject()
    {
        for (int i = 0; i < spawnNumber; i++)
        {
            GameObject newProp = Instantiate(GetRandomProp(), transform.position + GetRandomVolumePosition(), Quaternion.identity);
            //propsInWindTunnel.Add(newProp.GetComponent<Prop>());
        }
    }

    private GameObject GetRandomProp()
    {
        int randomIndex = Random.Range(0, propsToSpawn.Length);
        return propsToSpawn[randomIndex];
    }

    private Vector3 GetRandomVolumePosition()
    {
        float randX = Random.Range(-tunnelSize.x/2, tunnelSize.x/2);
        float randY = Random.Range(-tunnelSize.y / 2, tunnelSize.y / 2);
        float randZ = Random.Range(0, tunnelSize.z);

        Vector3 localX = randX * transform.right;
        Vector3 localY = randY * transform.up;
        Vector3 localZ = randZ * transform.forward;

        return localX + localY + localZ;
    }

    private Vector3 GetRandomStartPosition()
    {
        float randX = Random.Range(-tunnelSize.x / 2, tunnelSize.x / 2);
        float randY = Random.Range(-tunnelSize.y / 2, tunnelSize.y / 2);

        Vector3 localX = randX * transform.right;
        Vector3 localY = randY * transform.up;

        return localX + localY;
    }

}
