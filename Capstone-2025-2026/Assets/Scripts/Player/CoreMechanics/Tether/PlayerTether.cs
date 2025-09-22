using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    [SerializeField] private bool autoActivateTether = true;
    [SerializeField] private LayerMask tetherLayerMask;

    [Header("Current Tether Variables")]
    private TetherVisuals tetherVisuals;
    private TetherPull tetherPull;
    private TetherCollider tetherCollider;
    private GameObject currentTether;

    private Queue<GameObject> activeTethers = new Queue<GameObject>();

    private bool didHit = false;

    private void Awake()
    {
        controls = GetComponent<PlayerActions>();
        tetherPool = GetComponent<TetherPool>();
    }

    public void HandleStartTether(Tetherable heldObj = null)
    {

        Vector3 tetherPoint = Vector3.zero;
        Transform tetherTransform = null;
        didHit = false;

        if (heldObj == null)
        {
            Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            tetherPoint = ray.origin + ray.direction * maxTetherStartDist;

            if(Physics.Raycast(ray, out hit, maxTetherStartDist, ~tetherLayerMask))
            {
                if (tetherPool.AvailableTetherCount <= 0)
                {
                    currentTether = activeTethers.Dequeue();
                    currentTether.GetComponent<TetherPull>().ResetTether();
                }
                else
                {
                    currentTether = tetherPool.GetTether();
                    currentTether.GetComponent<TetherPull>().ResetTether();
                }

                didHit = true;

                tetherPoint = hit.point;
                tetherTransform = hit.transform;

                tetherPull = currentTether.GetComponent<TetherPull>();
                tetherVisuals = currentTether.GetComponent<TetherVisuals>();
                tetherCollider = currentTether.GetComponent<TetherCollider>();
                tetherPull.ResetTether();
                tetherVisuals.SetStartPoint(tetherPoint);
                tetherCollider.SetStartPoint(tetherPoint);
                tetherVisuals.PreviewPull();
                tetherPull.SetStartPoint(tetherTransform, tetherPoint);
            }
            else
            {
                currentTether = null;
                return;
            }
        }
        else
        {
            tetherPoint = heldObj.transform.position;
            tetherTransform = heldObj.transform;
        }
    }

    public void HandleTetherActive()
    {
        if (tetherVisuals != null && didHit)
        {
            Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            Vector3 tetherPoint = ray.origin + ray.direction * maxTetherStartDist;

            tetherVisuals.SetStartPoint(tetherPull.StartAttachPoint);
            tetherCollider.SetStartPoint(tetherPull.StartAttachPoint);

            if (Physics.Raycast(ray, out hit, maxTetherStartDist, ~tetherLayerMask))
            {
                tetherVisuals.SetEndPoint(hit.point);
                tetherCollider.SetEndPoint(hit.point);
            }
            else
            {
                tetherVisuals.SetEndPoint(tetherPoint); //if we don't hit anything, extend to max distance
                tetherCollider.SetEndPoint(tetherPoint);
            }
        }
    }

    public void HandleEndTether()
    {

        if (currentTether != null && didHit)
        {
            Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            Vector3 tetherPoint = ray.origin + ray.direction * maxTetherStartDist;

            if (Physics.Raycast(ray, out hit, maxTetherStartDist, ~tetherLayerMask))
            {
                if (hit.transform != tetherPull.StartTransform)
                {
                    tetherVisuals.SetEndPoint(hit.point);
                    tetherVisuals.IsSet = true;
                    tetherPull.SetEndPoint(hit.transform, hit.point);
                    tetherCollider.SetEndPoint(hit.point);
                    tetherCollider.IsSet = true;
                    activeTethers.Enqueue(currentTether);
                    if (autoActivateTether)
                    {
                        tetherVisuals.ActivatePull();
                        tetherPull.Activated = true;
                    }
                }
                else
                {
                    tetherVisuals.ResetPull();
                    tetherPool.ReturnTether(currentTether);
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

    public void ActivateSelectedTether()
    {
        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 tetherPoint = ray.origin + ray.direction * maxTetherStartDist;

        if (Physics.Raycast(ray, out hit, maxTetherStartDist, tetherLayerMask))
        {
            GameObject tether = hit.collider.transform.parent.gameObject;
            tether.GetComponent<TetherPull>().Activated = true;
            tether.GetComponent<TetherVisuals>().ActivatePull();
        }
    }
    public void ActivateAllTether()
    {
        GameObject[] plantedTethers = tetherPool.GetAllPlantedTether();

        foreach(GameObject tether in plantedTethers)
        {
            if(tether != null)
            {
                if(currentTether != null)
                    if(currentTether ==  tether) continue; 

                tether.GetComponent<TetherPull>().Activated = true;
                tether.GetComponent<TetherVisuals>().ActivatePull();
            }
        }
    }
}
