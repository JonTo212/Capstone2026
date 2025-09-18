using UnityEngine;

public class TetherCollider : MonoBehaviour
{
    [Header("Properties")]
    private Vector3 startPosition;
    private Vector3 endPosition;

    [Header("Components")]
    [SerializeField] private Transform capsuleTransform;
    [SerializeField] private CapsuleCollider capsuleCollider;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    //// Update is called once per frame
    //void FixedUpdate()
    //{
    //    capsuleCollider.height = Vector3.Distance(startPosition, endTransform);

    //    Vector3 aBVector = endTransform - startPosition;
    //    Vector3 perpendicularABVector = Vector3.Cross(aBVector, new Vector3(0, 1, 0));

    //    capsuleTransform = startPosition + aBVector / 2;

    //    capsuleTransform.rotation = Quaternion.LookRotation(perpendicularABVector, aBVector);
    //}

    //public void SetStartPoint()
    //{

    //}

    //public void SetEndPoint()
    //{

    //}
}
