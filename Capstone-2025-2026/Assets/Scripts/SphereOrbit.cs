using UnityEngine;

public class SphereOrbit : MonoBehaviour
{
    public Vector3 orbitSpeed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //rotate
        transform.eulerAngles += orbitSpeed * Time.deltaTime;
    }
}
