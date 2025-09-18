using System.Collections;
using System.Collections.Generic;
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

    private Queue<GameObject> activeTethers = new Queue<GameObject>();

    private void Awake()
    {
        controls = GetComponent<PlayerActions>();
        tetherPool = GetComponent<TetherPool>();
    }

    public void HandleStartTether(Tetherable heldObj = null)
    {
        if (tetherPool.AvailableTetherCount <= 0)
            currentTether = activeTethers.Dequeue();
        else
            currentTether = tetherPool.GetTether();

        Vector3 tetherPoint = Vector3.zero;
        Transform tetherTransform = null;

        if (heldObj == null)
        {
            Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            tetherPoint = ray.origin + ray.direction * maxTetherStartDist;

            if(Physics.Raycast(ray, out hit, maxTetherStartDist))
            {
                tetherPoint = hit.point;
                tetherTransform = hit.transform;
            }
        }
        else
        {
            tetherPoint = heldObj.transform.position;
            tetherTransform = heldObj.transform;
        }

        tetherPull = currentTether.GetComponent<TetherPull>();
        tetherVisuals = currentTether.GetComponent<TetherVisuals>();
        tetherPull.ResetTether();
        activeTethers.Enqueue(currentTether);
        tetherVisuals.SetStartPoint(tetherPoint);
        tetherVisuals.PreviewPull();
        tetherPull.SetStartPoint(tetherTransform, tetherPoint);
    }

    public void HandleTetherActive()
    {
        if (tetherVisuals != null)
        {
            Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            Vector3 tetherPoint = ray.origin + ray.direction * maxTetherStartDist;

            tetherVisuals.SetStartPoint(tetherPull.StartAttachPoint);

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
                if (hit.transform != tetherPull.StartTransform.transform)
                {
                    tetherVisuals.SetEndPoint(hit.point);
                    tetherVisuals.ActivatePull();

                    tetherPull.SetEndPoint(hit.transform, hit.point);

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
                activeTethers.Dequeue();
            }

            //remove references to current tether, but don't return it to the pool as it's now active
            tetherPull = null;
            tetherVisuals = null;
            currentTether = null;
        }
    }
}
