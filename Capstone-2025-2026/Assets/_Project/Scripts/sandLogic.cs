using UnityEngine;

public class sandLogic : MonoBehaviour
{
    public bool isSolid = false;
    public float shrinkSpeed = 0.1f;    

    private Material material;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        material = GetComponent<Renderer>().material;
        material.color = Color.white;
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.name == "WaterBlock")
        {
            isSolid = true;
            material.color = Color.gray;
        }
    }


    private void OnTriggerStay(Collider other)
    {
        if (other.name == "WindAura")
        {

            if (!isSolid) Shrink();
        }
    }

    public void Shrink()
    {
        //Shrink object
        transform.localScale -= new Vector3(shrinkSpeed, shrinkSpeed, shrinkSpeed) * Time.deltaTime;

        //destroy if small enough

        if (transform.localScale.x <= 0 || transform.localScale.y <= 0 || transform.localScale.z <= 0)
        {
            Destroy(gameObject);
        }

    }
}

