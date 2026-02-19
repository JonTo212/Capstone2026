using UnityEngine;

public class planeCrashSetpiece : MonoBehaviour
{
    //private InCameraDetector inCameraDetectorScript;
    //public Animator planeCrashAnimator;

    //public bool hasEntererdView = false;
    public GameObject vesselDummy;
    public GameObject vesselReal;

    public bool ReadyToLook = false;
    private MeshRenderer mr;

    public float fallSpeed = 60f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mr = vesselDummy.GetComponent<MeshRenderer>();
        mr.enabled = false;

        //inCameraDetectorScript = GetComponent<InCameraDetector>();

        vesselReal.SetActive(false);

    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            mr.enabled = true;
            ReadyToLook = true;
        }

        
    }

    // Update is called once per frame
    void Update()
    {
        if (ReadyToLook)
        {
            Crash();
            /*
            if (inCameraDetectorScript.isInCameraView)
            {
                hasEntererdView = true;

                if (hasEntererdView)
                {
                    Crash();
                }
            }
            */
        }

        if (vesselDummy.transform.position.y < 70)
        {
            Destroy(vesselDummy.gameObject);
            vesselReal.SetActive(true);

            print("VesselCrash");
            //playsound here
        }
    }


    public void Crash()
    {
        vesselDummy.transform.position += new Vector3(0, -fallSpeed, 0) * Time.deltaTime;

        vesselDummy.transform.Rotate(fallSpeed*2*Time.deltaTime, fallSpeed * Time.deltaTime, fallSpeed * 2 * Time.deltaTime);
    }
}
