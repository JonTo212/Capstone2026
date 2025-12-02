using UnityEngine;

public class spinscript : MonoBehaviour
{
    public float ySpeed = 90f; // degrees per second

    void Update()
    {
        transform.Rotate(0f, ySpeed * Time.deltaTime, 0f);
    }
}
