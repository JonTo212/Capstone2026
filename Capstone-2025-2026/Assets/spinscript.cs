using UnityEngine;

public class spinscript : MonoBehaviour
{
    public float ySpeedMax = 90f; // degrees per second
    public float ySpeedMin = 45f;

    private float ySpeed;

    private void Start()
    {
       ySpeed = Random.Range(ySpeedMin, ySpeedMax);
    }

    void Update()
    {


        transform.Rotate(0f, ySpeed * Time.deltaTime, 0f);
    }
}
