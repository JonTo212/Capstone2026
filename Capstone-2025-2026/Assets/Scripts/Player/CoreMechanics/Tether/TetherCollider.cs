using Unity.VisualScripting;
using UnityEngine;

public class TetherCollider : MonoBehaviour
{
    [Header("Properties")]
    private Vector3 startPoint;
    private Vector3 endPoint;
    private bool isSet;

    [Header("Components")]
    [SerializeField] private Transform capsuleTransform;
    [SerializeField] private CapsuleCollider capsuleCollider;
    [SerializeField] private TetherPull tetherPull;

    public bool IsSet
    {
        get { return isSet; }
        set { isSet = value; } 
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    private void FixedUpdate()
    {
        if(isSet)
        {
            SetStartPoint(tetherPull.StartAttachPoint);
            SetEndPoint(tetherPull.EndAttachPoint);
            UpdateCollider();
        }
    }

    public void UpdateCollider()
    {
        capsuleCollider.height = Vector3.Distance(startPoint, endPoint);

        Vector3 startEndVector = endPoint - startPoint;
        Vector3 startEndNormal = Vector3.Cross(startEndVector, new Vector3(0, 1, 0));

        capsuleTransform.position = startPoint + startEndVector / 2;

        capsuleTransform.rotation = Quaternion.LookRotation(startEndNormal, startEndVector);
    }

    public void SetStartPoint(Vector3 StartPoint)
    {
        startPoint = StartPoint;
    }

    public void SetEndPoint(Vector3 EndPoint)
    {
        endPoint = EndPoint;
    }
}
