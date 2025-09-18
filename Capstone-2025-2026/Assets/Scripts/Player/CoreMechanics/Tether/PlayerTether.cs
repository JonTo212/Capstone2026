using UnityEngine;

public class PlayerTether : MonoBehaviour
{
    [Header("Components")]
    private PlayerActions controls;
    private TetherPool tetherPool;
    [SerializeField] private Camera playerCam;
    private RaycastHit hit;

    [Header("Properties")]
    [SerializeField] private float maxTetherStartDist = 50f;

    [Header("Current Tether Variables")]
    private TetherVisuals tetherVisuals;
    private TetherPull tetherPull;
    private GameObject currentTether;

    private void Awake()
    {
        controls = GetComponent<PlayerActions>();
        tetherPool = GetComponent<TetherPool>();
    }

    public void HandleStartTether(Tetherable heldObj = null)
    {
        if (tetherPool.AvailableTetherCount <= 0) return;

        Vector3 tetherPoint = Vector3.zero;
        Rigidbody tetherBody = null;

        if (heldObj == null)
        {
            Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            tetherPoint = ray.origin + ray.direction * maxTetherStartDist;

            if(Physics.Raycast(ray, out hit, maxTetherStartDist))
            {
                tetherPoint = hit.point;
                tetherBody = hit.rigidbody;
            }
        }
        else
        {
            tetherPoint = heldObj.transform.position;
            tetherBody = heldObj.Rb;
        }

        currentTether = tetherPool.GetTether();
        tetherPull = currentTether.GetComponent<TetherPull>();
        tetherVisuals = currentTether.GetComponent<TetherVisuals>();
        tetherVisuals.SetStartPoint(tetherPoint);
        tetherVisuals.PreviewPull();
        tetherPull.SetStartPoint(tetherBody, tetherPoint);
    }

    public void HandleTetherActive()
    {
        if (tetherVisuals != null)
        {
            Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            Vector3 tetherPoint = ray.origin + ray.direction * maxTetherStartDist;

            if (Physics.Raycast(ray, out hit, maxTetherStartDist))
            {
                tetherVisuals.SetEndPoint(hit.point);
            }
            else
            {
                tetherVisuals.SetEndPoint(tetherPoint); //if we don't hit anything, extend to max distance
            }
        }
    }

    public void HandleEndTether()
    {
        if (currentTether != null)
        {
            Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            Vector3 tetherPoint = ray.origin + ray.direction * maxTetherStartDist;

            if (Physics.Raycast(ray, out hit, maxTetherStartDist))
            {
                if (hit.transform != tetherPull.StartRb.transform)
                {
                    tetherVisuals.SetEndPoint(hit.point);
                    tetherVisuals.ActivatePull();

                    if (hit.rigidbody != tetherPull.StartRb)
                    {
                        tetherPull.SetEndPoint(hit.rigidbody, hit.point);
                    }
                    else
                    {
                        tetherVisuals.ResetPull();
                    }

                    tetherPull.Activated = true;
                }
                else
                {
                    tetherVisuals.ResetPull();
                }
            }
            else
            {
                tetherPool.ReturnTether(currentTether);
            }

            //remove references to current tether, but don't return it to the pool as it's now active
            tetherPull = null;
            tetherVisuals = null;
            currentTether = null;
        }
    }
}
