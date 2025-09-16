using UnityEngine;

public class PlayerTether : MonoBehaviour
{
    private PlayerActions controls;
    private TetherVisuals tetherVisuals;
    private GameObject currentTether;
    [SerializeField] private GameObject tetherPullPrefab;
    private TetherPull tetherPull;
    [SerializeField] private Camera playerCam;
    private float maxTetherStartDist = 50f;

    private void Awake()
    {
        controls = GetComponent<PlayerActions>();
    }

    private void Update()
    {
        if(controls.ThrowDown)
        {
            if (Physics.Raycast(playerCam.transform.position, playerCam.transform.forward, out RaycastHit hit)) 
            {
                currentTether = Instantiate(tetherPullPrefab, transform.position, Quaternion.identity);
                tetherPull = currentTether.GetComponent<TetherPull>();
                tetherVisuals = currentTether.GetComponent<TetherVisuals>();
                tetherVisuals.SetStartPoint(hit.point);
                tetherVisuals.PreviewPull();
                tetherPull.SetStartObj(hit.transform);
            }
        }

        if (controls.ThrowHeld)
        {
            Physics.Raycast(playerCam.transform.position, playerCam.transform.forward, out RaycastHit hit);
            tetherVisuals.SetEndPoint(hit.point);
        }

        if(controls.ThrowUp)
        {
            if (tetherPull.StartObj != null)
            {
                if (Physics.Raycast(playerCam.transform.position, playerCam.transform.forward, out RaycastHit hit))
                {
                    tetherVisuals.SetEndPoint(hit.point);
                    tetherVisuals.ActivatePull();
                    tetherPull.SetEndObj(hit.transform);
                    tetherPull.Activated = true;
                }
                else
                {
                    CancelTether();
                }
            }
        }
    }

    private void CancelTether()
    {
        if (tetherPull.EndObj == null)
        {
            tetherPull = null;
            tetherVisuals = null;
            Destroy(currentTether);
        }
    }
}
