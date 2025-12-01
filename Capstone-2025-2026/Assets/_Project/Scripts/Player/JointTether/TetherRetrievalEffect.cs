using System;
using UnityEngine;

public class TetherRetrievalEffect : MonoBehaviour
{
    [SerializeField] Vector3 startPosition;
    [SerializeField] Transform endTarget;
    [SerializeField] float timeToRetrieve = 1f;
    [SerializeField] float retrieveOffset = 2f;
    [SerializeField] float retrieveOffsetMaxRange = 5f;
    [SerializeField] AnimationCurve speedCurve;
    [SerializeField] AnimationCurve offsetCurve;

    private float currentTime;
    private Vector3 offsetDirection;
    private float offsetAmount;

    public event Action onRetrievalFinished;

    public void Init(Vector3 startPosition, Transform endTarget)
    {
        this.startPosition = startPosition;
        this.endTarget = endTarget;

        Vector3 startEndVector = startPosition - endTarget.position;
        Vector3 startEndDirection = startEndVector.normalized;
        Vector3 localRight = Vector3.Cross(startEndDirection, Vector3.up);
        Vector3 localUp = Vector3.Cross(startEndDirection,localRight);
        float randomHorizonal = ((int)UnityEngine.Random.Range(0.01f, 1.99f)) - 1;
        offsetDirection = localUp + localRight * randomHorizonal;
        float startEndDistance = startEndDirection.magnitude;
        offsetAmount = Mathf.Lerp(0, retrieveOffset, Mathf.Clamp01(startEndDistance / retrieveOffsetMaxRange));
    }

    // Update is called once per frame
    void Update()
    {
        currentTime += Time.deltaTime;

        float retrieveAlpha = speedCurve.Evaluate(currentTime / timeToRetrieve);
        float offsetAlpha = offsetCurve.Evaluate(currentTime / timeToRetrieve);

        transform.position = Vector3.Lerp(startPosition, endTarget.position, retrieveAlpha) + offsetDirection * Mathf.Lerp(0, offsetAmount, offsetAlpha);
        
        if(currentTime > timeToRetrieve)
        {
            Destroy(gameObject);
        }
        onRetrievalFinished?.Invoke();
    }
}
