using FMODUnity;
using NodeCanvas.Tasks.Actions;
using Unity.VisualScripting;
using UnityEngine;

public class clawGrabber : MonoBehaviour
{
    private EnvironmentalProp environmentalPropScript;
    public GameObject grabbedObject;
    
    public FixedJoint fixedJoint;

    public GameObject parent;
    private Rigidbody parentRb;

    public Transform retractPoint;
    public float retractSpeed;

    public bool isGrabbed = false;

    public AudioManager audioManager;

    public float clickSoundDistance = 2;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        environmentalPropScript = parent.GetComponent<EnvironmentalProp>();
        parentRb = parent.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        parentRb.angularVelocity = new Vector3(0, 0, 0);

        isGrabbed = environmentalPropScript.IsSnared;

        if (!isGrabbed)
        {
            parentRb.useGravity = false;
            parentRb.linearVelocity = new Vector3(0, 0, 0);

            //move towards retract point
            parent.transform.position = Vector3.MoveTowards(parent.transform.position,retractPoint.position,retractSpeed*Time.deltaTime);
        }

        //play sound every 10 meters
        var dist = retractPoint.position - transform.position;
        print (dist.magnitude);

        //play sound when extending
        if (dist.magnitude > clickSoundDistance+4) // needs a small buffer zone so the extending and retracting dont fight over subtracting and adding
        {

            RuntimeManager.PlayOneShot("event:/MenuSelect", transform.position);
            Debug.Log("clawGrabber.cs");
            //audioManager.PlaySFX(audioManager.GrabberClick, 1, 1);

            clickSoundDistance += 2f;
        }

        //playsound when retracting
        if (dist.magnitude < clickSoundDistance-4)
        {
            Debug.Log("clawGrabber.cs");
            RuntimeManager.PlayOneShot("event:/MenuSelect", transform.position);

            clickSoundDistance -= 2f;
        }



    }

    private void OnTriggerEnter(Collider other)
    {
        if (fixedJoint.connectedBody != null) fixedJoint.connectedBody = null;

        //check the claw is being picked up by player
        if (isGrabbed)
        {
            print("holding");

            //make sure target is a prop
            if (other.GetComponent<EnvironmentalProp>()!=null)
            {
                //get components of target
                var propScript = other.GetComponent<EnvironmentalProp>();
                var propRb = other.GetComponent<Rigidbody>();

                //add target rb to fixed joint
                propRb.useGravity = false;
                propRb.freezeRotation = true;

                fixedJoint.connectedBody = propRb;

            }
        }

    }
}
