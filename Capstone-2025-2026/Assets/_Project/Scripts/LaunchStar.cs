using System.Collections;
using UnityEngine;

public class LaunchStar : MonoBehaviour
{
    public EnvironmentalProp environmentalPropScript;
    public Transform endPoint;
    public float Speed = 2f;

    public GameObject player;
    public GameObject star;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (environmentalPropScript.IsSnared)
        {
            StartCoroutine(Launch());
        }

        print(Vector3.Distance(transform.position, endPoint.position));
    }

    IEnumerator Launch()
    {

        while (Vector3.Distance(transform.position, endPoint.position) > 0.1f)
        {
            //make player child of the star so it moves with it
            player.transform.SetParent(transform);

            transform.position = Vector3.MoveTowards(transform.position, endPoint.position, Speed * Time.deltaTime);
            

            yield return null;
        }
    }
}
