using FMODUnity;
using UnityEngine;

public class planeCrashSetpiece : MonoBehaviour
{
    //private InCameraDetector inCameraDetectorScript;
    //public Animator planeCrashAnimator;

    private bool hasPlayed = false;

    //public bool hasEntererdView = false;
    public GameObject vesselDummy;

    [SerializeField] GameObject vesselReal;

    //sounds
    public GameObject vesselCrashSound; //MUST START DISABLED
    public GameObject vesselFallSound; //MUST START DISABLED

    public bool ReadyToLook = false;
    private MeshRenderer mr;

    public float fallSpeed = 60f;


    [Header("Tether Tutorial Cutscene Variables")] //Used for the tether wall puzzle cutscene
    [SerializeField] private Transform cutsceneStartPos;
    [SerializeField] private Transform lookAtTarget;
    [SerializeField] private float cutsceneDuration;
    [SerializeField] private float cutsceneHoldFraction;
    [SerializeField] private float cutsceneBlendInDelay;
    [SerializeField] private float cutsceneBlendInTime;
    [SerializeField] private EndSequeenceTracker endTrack;

    [SerializeField] private VesselCrashCutscene vesselCrashCutsceneScript;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mr = vesselDummy.GetComponent<MeshRenderer>();
        vesselCrashCutsceneScript = GetComponent<VesselCrashCutscene>();
        mr.enabled = false;


        vesselCrashSound.SetActive(false);
        vesselFallSound.SetActive(false);
        vesselReal.SetActive(false);
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!hasPlayed)
            {
                mr.enabled = true;
                ReadyToLook = true;

                // cutscene
                vesselCrashCutsceneScript.Configure(cutsceneStartPos, lookAtTarget, cutsceneDuration, cutsceneHoldFraction, cutsceneBlendInDelay, cutsceneBlendInTime);
                CameraRefData.Instance.CameraCutsceneHandler.StartCutscene(vesselCrashCutsceneScript);

                hasPlayed = true;
            }   
        }
    }

    // Update is called once per frame
    void Update()
    {    
        if (ReadyToLook)
        {
            StartFall();
        }

        //Crashed
        if (vesselDummy!=null)
        {
            if (vesselDummy.transform.position.y < 70)
            {
                //destroy dummy
                Destroy(vesselDummy.gameObject);

                //set real vessel active
                vesselReal.SetActive(true);
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
