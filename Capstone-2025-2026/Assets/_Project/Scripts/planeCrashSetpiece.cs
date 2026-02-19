using UnityEngine;

public class planeCrashSetpiece : MonoBehaviour
{
    private InCameraDetector inCameraDetectorScript;
    //public Animator planeCrashAnimator;

    public bool hasEntererdView = false;
    public bool ReadyToLook = false;
    private MeshRenderer mr;
    public Collider trigger;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mr = GetComponent<MeshRenderer>();
        inCameraDetectorScript = GetComponent<InCameraDetector>();

        mr.enabled = false;
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

        if (transform.position.y < -8)
        {
            Destroy(gameObject);
        }
    }


    public void Crash()
    {
        transform.position += new Vector3(0, -25, 100) * Time.deltaTime;
    }
}
