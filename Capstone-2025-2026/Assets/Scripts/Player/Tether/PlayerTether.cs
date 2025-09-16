using UnityEngine;

public class PlayerTether : MonoBehaviour
{
    [Header("Components")]
    private PlayerActions controls;
    [SerializeField] private GameObject tetherPullPrefab;
    [SerializeField] private Camera playerCam;

    [Header("Properties")]
    [SerializeField] private float maxTetherStartDist = 50f;

    [Header("Current Tether Variables")]
    private TetherVisuals tetherVisuals;
    private TetherPull tetherPull;
    private GameObject currentTether;

    private void Awake()
    {
        controls = GetComponent<PlayerActions>();
    }

    private void Update()
    {
        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 tetherPoint = ray.origin + ray.direction * maxTetherStartDist;
        bool tetherCast = Physics.Raycast(ray, out RaycastHit hit, maxTetherStartDist);

        if (controls.ThrowDown)
        {
            if (tetherCast) 
            {
                currentTether = Instantiate(tetherPullPrefab, transform.position, Quaternion.identity);
                tetherPull = currentTether.GetComponent<TetherPull>();
                tetherVisuals = currentTether.GetComponent<TetherVisuals>();
                tetherVisuals.SetStartPoint(hit.point);
                tetherVisuals.PreviewPull();
                tetherPull.SetStartPoint(hit.rigidbody, hit.point);
            }
        }

        if (controls.ThrowHeld)
        {
            if (tetherCast)
            {
                tetherVisuals.SetEndPoint(hit.point);
            }
            else
            {
                tetherVisuals.SetEndPoint(tetherPoint);
            }
        }

        if(controls.ThrowUp)
        {
            if (tetherCast)
            {
                tetherVisuals.SetEndPoint(hit.point);
                tetherVisuals.ActivatePull();
                tetherPull.SetEndPoint(hit.rigidbody, hit.point);
                tetherPull.Activated = true;
            }
            else
            {
                CancelTether();
            }
        }
    }

    private void CancelTether()
    {
        tetherPull = null;
        tetherVisuals = null;
        Destroy(currentTether);
    }
}
