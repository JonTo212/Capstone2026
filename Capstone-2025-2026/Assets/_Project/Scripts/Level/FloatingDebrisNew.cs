using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class FloatingDebrisNew : MonoBehaviour
{
    private Tetherable tetherableScript;

    private GameObject target;
    public float baseOrbitSpeed;
    private float orbitSpeed;
    public float orbitSpeedVariation;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        target = transform.parent.gameObject;

        //random orbit speed
        orbitSpeed = Random.Range(baseOrbitSpeed-orbitSpeedVariation, baseOrbitSpeed + orbitSpeedVariation);


        //tetherscrpit
        tetherableScript = GetComponent<Tetherable>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //check if object is held
        if (tetherableScript.isHeld)
        {
            if (transform.parent !=null) transform.parent=null;
        }

        // Spin the object around the target
        transform.RotateAround(new Vector3 (target.transform.position.x, target.transform.position.y, target.transform.position.z), Vector3.up, orbitSpeed * Time.deltaTime);
    }
}
