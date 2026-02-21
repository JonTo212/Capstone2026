using UnityEngine;

public class GrabIndicator : MonoBehaviour
{
    [Header("Grab Indicator")]
    [SerializeField] private Camera playerCam;


    [SerializeField] private Lasso lassoScript;

    [SerializeField] private PickupNPCProp pickupNPCPropScript;

    [SerializeField] private GameObject GrabIndicatorObject;
    [SerializeField] private float indicatorDistance;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lassoScript = GameObject.FindWithTag("Player").GetComponent<Lasso>();

        indicatorDistance = lassoScript.MaxLassoRange; // the minus 1 is bc the offset of the lasso is weird, this needs to be changed later to be more accurate

        playerCam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        GrabIndicatorLogic(indicatorDistance);
    }

    void GrabIndicatorLogic(float indicatorStartDist) // indicator hint is the small shape that shows it can be grabbed, the indicator start shows its ready to be grabbed
    {
        float dist = Vector3.Distance(playerCam.transform.position, transform.position);

        if (dist < indicatorStartDist)
        {
            GrabIndicatorObject.SetActive(true);
        }
        else
        {
            GrabIndicatorObject.SetActive(false);
        }
    }
}
