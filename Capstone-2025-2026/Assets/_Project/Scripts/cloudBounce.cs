using UnityEngine;

public class cloudBounce : MonoBehaviour
{
    public float minSpeed = 0.5f;
    public float maxSpeed = 2.0f;
    public Vector3 offset = new Vector3(0, 2, 0);

    private float _speed;
    private Vector3 _startPos;

    void Start()
    {
        // 1. Set the random speed
        _speed = Random.Range(minSpeed, maxSpeed);

        // 2. Store the starting position so we bounce around a fixed point
        _startPos = transform.position;
    }

    void Update()
    {
        // PingPong returns a value between 0 and 1
        float t = Mathf.PingPong(Time.time * _speed, 1);

        // 3. Lerp between (Start + Offset) and (Start - Offset)
        transform.position = Vector3.Lerp(_startPos + offset, _startPos - offset, t);
    }
}
