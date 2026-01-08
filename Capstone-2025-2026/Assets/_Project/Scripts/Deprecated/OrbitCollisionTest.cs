using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.Shapes;

public class OrbitCollisionTest : MonoBehaviour
{
    [SerializeField] private GameObject target;
    [SerializeField] private Material material;

    [SerializeField] private float maxDist;
    [SerializeField] private float minDist;
    [SerializeField] private float maxHeight;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //get distance between self and target
        var selfPos = new Vector2 (transform.position.x, transform.position.z);
        var targetPos = new Vector2 (target.transform.position.x, target.transform.position.z);

        var diff = Vector2.Distance(selfPos, targetPos);
        var heightDiff = Mathf.Abs(transform.position.y-target.transform.position.y);


        //check distance
        if ((diff < maxDist) && (diff > minDist) && (heightDiff < maxHeight)) /*&& target.GetComponent<Rigidbody>().linearVelocity.magnitude > 10f*/
        {
            material.color = Color.green;
        }
        else
        {
            material.color = Color.purple;
        }

    }
}
