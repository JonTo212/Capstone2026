using UnityEngine;

public class BounceRock : MonoBehaviour
{
    public bool bounceActivate = false;
    public float bounceForce = 100f;

    public float size = 2f;

    public float collisionCount;

    private RockState currentRockState;

    public enum RockState
    {
        Idle,
        Primed,
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        DetectionRadius(size);

        HandleRockState();

        print("Collisions: " + collisionCount);

        //draw sphere for detection radius
        Debug.DrawRay(transform.position, Vector3.up * size, Color.red);

    }

    private void HandleRockState()
    {
        switch (currentRockState)
        {
            case RockState.Idle:
                HandleIdleState();
                break;

            case RockState.Primed:
                HandlePrimedState();
                break;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {

        }
    }

    void HandleIdleState()
    {
        if (collisionCount <= 0)
        {
            currentRockState = RockState.Primed;
        }
    }

    void HandlePrimedState()
    {
        print("Rock Primed");
    }

    void DetectionRadius(float radius)
    {

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);
        foreach (var hitCollider in hitColliders)
        {

            
        }
    }
}
