using UnityEngine;

public class godRay : MonoBehaviour
{

    public GameObject player;

    private Material mat;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mat = GetComponent<Renderer>().material;
    }

    // Update is called once per frame
    void Update()
    {

        //adjust transparency based on distance to player
        float distance = Vector3.Distance(transform.position, player.transform.position);


        


    }
}
