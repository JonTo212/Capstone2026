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

    private void Update()
    {
        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 tetherPoint = ray.origin + ray.direction * maxTetherStartDist;
        bool tetherCast = Physics.Raycast(ray, out hit, maxTetherStartDist);

        if (controls.ThrowDown && tetherCast)
        {
            if (tetherPool.AvailableTetherCount <= 0) return;

            HandleStartTether();
        }

        if (controls.ThrowHeld && tetherVisuals != null)
        {
            if (tetherCast)
            {
                tetherVisuals.SetEndPoint(hit.point);
            }
            else
            {
                tetherVisuals.SetEndPoint(tetherPoint); //if we don't hit anything, extend to max distance
            }
        }

        if(controls.ThrowUp && currentTether != null)
        {
            if (tetherCast)
            {
                HandleEndTether();
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

    private void HandleStartTether()
    {
        currentTether = tetherPool.GetTether();
        tetherPull = currentTether.GetComponent<TetherPull>();
        tetherVisuals = currentTether.GetComponent<TetherVisuals>();
        tetherVisuals.SetStartPoint(hit.point);
        tetherVisuals.PreviewPull();
        tetherPull.SetStartPoint(hit.rigidbody, hit.point);
    }

    private void HandleEndTether()
    {
        tetherVisuals.SetEndPoint(hit.point);
        tetherVisuals.ActivatePull();

        if (hit.rigidbody != tetherPull.StartRb)
        {
            tetherPull.SetEndPoint(hit.rigidbody, hit.point);
        }
        else
        {
            tetherPull.SetEndPoint(null, hit.point); //if we hit the same object, just tether to the world at that point
        }

        tetherPull.Activated = true;
    }
}
