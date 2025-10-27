using UnityEngine;

public class IslandBob : MonoBehaviour
{
    [SerializeField] private float oscillationRange;
    [SerializeField] private float oscillationSpeed;

    private void Update()
    {
        float newY = Mathf.Sin(Time.time * oscillationSpeed) * oscillationRange;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}
