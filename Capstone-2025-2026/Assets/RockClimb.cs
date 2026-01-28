using UnityEngine;

public class RockClimb : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private PlayerActions playerActionsScript;

    [SerializeField] private bool isConnected;
    [SerializeField] private GameObject connectedObject;
    [SerializeField] private float wallJumpForce;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerActionsScript = GetComponent<PlayerActions>();

    }

    // Update is called once per frame
    void Update()
    {
        // if player isconnected and presses jump button
        if ((isConnected) && (playerActionsScript.JumpDown))
        {
            //remove connection 
            var fixedJoint = connectedObject.GetComponent<FixedJoint>();
            fixedJoint.connectedBody = null;

            //force jump
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * wallJumpForce, ForceMode.Impulse);
            AudioManager.Instance.PlaySFX(AudioManager.Instance.Jump, 6, 1f);

            //destroy holder
            Destroy(connectedObject.transform.parent.gameObject);

            //connected false
            isConnected = false;
            connectedObject = null;
        }

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("RockClimb") && (!isConnected))
        {
            connectedObject = other.gameObject;
            var fixedJoint = connectedObject.GetComponent<FixedJoint>();

            fixedJoint.connectedBody = rb;
            isConnected = true;
        }
    }


}
