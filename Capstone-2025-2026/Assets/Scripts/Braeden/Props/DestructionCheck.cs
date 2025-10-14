using UnityEngine;

public class DestructionCheck : MonoBehaviour
{
    public float speed = 0;

    public float terminalVelocity = 5;
    public bool canDestroy = false;

    Vector3 lastPosition;

    private void Start()
    {
        lastPosition = transform.position;
    }

    private void FixedUpdate()
    {
        CalculateSpeed();

        if (speed > terminalVelocity)
        {
            //destroy object
            canDestroy = true;
        }
        else canDestroy = false;
    }

    private void CalculateSpeed()
    {
        //calculate speed 
        speed = (transform.position - lastPosition).magnitude / Time.deltaTime;
        lastPosition = transform.position;

        //print(speed);
    }

}
