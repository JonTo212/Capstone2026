using System.Collections.Generic;
using UnityEngine;

public class WindTunnel : MonoBehaviour
{
    [SerializeField] private float windStrength = 5f;
    [SerializeField] private float windSpeed = 5f;
    [SerializeField] private Vector3 windDirection;
    [SerializeField] private Vector3 tunnelSize = Vector3.one * 3;
    [SerializeField] private List<Prop> propsInWindTunnel = new List<Prop>();
    [SerializeField] private BoxCollider boxCollider;

    [Header("Debug")]
    [SerializeField] private Mesh debugArrow;
    [SerializeField] private Mesh debugCube;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        windDirection = transform.forward;
        boxCollider.center = tunnelSize / 2;
        boxCollider.size = tunnelSize;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawMesh(debugArrow, transform.position, Quaternion.identity, Vector3.one * 0.5f);
        Gizmos.DrawMesh(debugCube, transform.position + transform.forward * (tunnelSize.z/2), Quaternion.identity, tunnelSize);
    }

    private void FixedUpdate()
    {
        foreach (Prop prop in propsInWindTunnel)
        {
            prop.ApplyForceInDirection(transform.forward, windSpeed, ForceMode.Force);
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
            propsInWindTunnel.Remove(other.GetComponent<Prop>());
            other.GetComponent<Rigidbody>().useGravity = true;
        }
    }
}
