using FMODUnity;
using UnityEngine;

public class planeCrashSetpiece : MonoBehaviour
{
    //private InCameraDetector inCameraDetectorScript;
    //public Animator planeCrashAnimator;

    //public bool hasEntererdView = false;
    public GameObject vesselDummy;
    public GameObject vesselReal;

    //sounds
    public GameObject vesselCrashSound; //MUST START DISABLED
    public GameObject vesselFallSound; //MUST START DISABLED

    public bool ReadyToLook = false;
    private MeshRenderer mr;

    public float fallSpeed = 60f;

    [SerializeField] private ParticleSystem dustParticle;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mr = vesselDummy.GetComponent<MeshRenderer>();
        mr.enabled = false;

        //inCameraDetectorScript = GetComponent<InCameraDetector>();

        vesselReal.SetActive(false);
        vesselCrashSound.SetActive(false);
        vesselFallSound.SetActive(false);

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
            StartFall();
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

        //Crashed
        if (vesselDummy!=null)
        {
            if (vesselDummy.transform.position.y < 70)
            {

                Destroy(vesselDummy.gameObject);

                //playsound here
                vesselCrashSound.SetActive(true);
                //RuntimeManager.PlayOneShot("event:/WallBreak", vesselReal.transform.position);

                vesselReal.SetActive(true);

                print("VesselCrash");

                //Hit Ground
                dustParticle.Play();

            }
        }
  
    }


    public void StartFall()
    {
        //playsound here
        vesselFallSound.SetActive(true);

        vesselDummy.transform.position += new Vector3(0, -fallSpeed, 0) * Time.deltaTime;

        vesselDummy.transform.Rotate(fallSpeed*2*Time.deltaTime, fallSpeed * Time.deltaTime, fallSpeed * 2 * Time.deltaTime);
    }
}
