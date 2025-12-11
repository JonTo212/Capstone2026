using UnityEngine;

public class TreeScript : MonoBehaviour
{
    private float bendAmount = 1f;
    public bool startWithRandomBend = true;  

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //set random rotation between -bendAmount and bendAmount on the x and z axis
        float randomX = Random.Range(-bendAmount, bendAmount);
        float randomZ = Random.Range(-bendAmount, bendAmount);
        transform.rotation = Quaternion.Euler(randomX, 0, randomZ);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
