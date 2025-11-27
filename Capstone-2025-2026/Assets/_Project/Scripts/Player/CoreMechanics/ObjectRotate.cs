using UnityEngine;

public class ObjectRotate : MonoBehaviour
{
    public static ObjectRotate Instance { get; private set; }

    [SerializeField] private float degreesPerSecond = 90f;
    [SerializeField] private float yawSens;
    [SerializeField] private float pitchSens;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void RotateWithInput(Transform objectToRotate, Vector2 input, Vector3 yawAxis, Vector3 pitchAxis, bool localSpace)
    {
        if (objectToRotate == null) return;

        float stepX = input.x * degreesPerSecond * yawSens * 0.05f * Time.deltaTime;
        float stepY = input.y * degreesPerSecond * pitchSens * 0.05f * Time.deltaTime;

        if (localSpace)
        {
            objectToRotate.Rotate(yawAxis, stepX, Space.Self);
            objectToRotate.Rotate(pitchAxis, -stepY, Space.Self);
        }
        else
        {
            objectToRotate.Rotate(yawAxis, stepX, Space.World);
            objectToRotate.Rotate(pitchAxis, -stepY, Space.World);
        }
    }
}
