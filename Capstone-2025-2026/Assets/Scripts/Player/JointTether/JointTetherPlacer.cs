using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class JointTetherPlacer : MonoBehaviour
{
    //[SerializeField] 
    private PlayerActions _playerActions;
    private Camera _playerCamera;

    [Header("Tether Preview Line Properties")]
    [SerializeField] private GameObject tetherPreviewLinePrefab;
    private TetherPreviewLine tetherPreviewLine;

    [Header("Properties")]
    [SerializeField] private GameObject jointTetherPrefab;
    [SerializeField] private LayerMask tetherLayerMask;
    [SerializeField] private int numOfTethersPlaced = 0;
    [SerializeField] private bool autoActivateTether = true;
    private List<JointTether> allTethers = new List<JointTether>();
    private bool didStartPointHit = false;
    private bool didEndPointHit = false;

    [Header("Editable Properties")]
    [SerializeField] private float maxTetherStartDist = 50f;
    [SerializeField] private int maxNumOfTethers = 3;
    [SerializeField] private float timeToActivateAllTethers = 0.8f;
    [SerializeField] private float timeToDestroyAllTethers = 0.8f;

    [Header("Hit Properties")]
    private Transform firstHitTransform;
    private Vector3 firstHitPosition;
    private Transform secondHitTransform;
    private Vector3 secondHitPosition;

    [Header("Coroutines")]
    private Coroutine activateAllTethersCoroutine;
    private Coroutine destroyAllTethersCoroutine;

    private void Awake()
    {
        _playerActions = GetComponent<PlayerActions>();
        _playerCamera = Camera.main;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(numOfTethersPlaced < maxNumOfTethers)
        {
            if(_playerActions.ThrowDown)
            {
                CreateTetherPreviewLine();
            }
            else if(_playerActions.ThrowHeld && didStartPointHit)
            {
                UpdateTetherPreviewLine();
            }
            else if(_playerActions.ThrowUp && didStartPointHit)
            {
                CreateAndInitTether();
                DeletePreviewTetherLine();
            }
        }

        if(_playerActions.InteractDown)
        {
            Ray ray = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, tetherLayerMask, QueryTriggerInteraction.Collide))
            {
                hit.transform.GetComponent<JointTether>().ActivateTether();
            }

            activateAllTethersCoroutine = StartCoroutine(ActivateAllTether());
        }

        if(_playerActions.InteractUp)
        {
            StopCoroutine(activateAllTethersCoroutine);
        }

        if(_playerActions.CrouchDown)
        {
            Ray ray = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, tetherLayerMask, QueryTriggerInteraction.Collide))
            {
                allTethers.Remove(hit.transform.GetComponent<JointTether>());
                hit.transform.GetComponent<JointTether>().DestroyTether();
            }

            destroyAllTethersCoroutine = StartCoroutine(DestroyAllTether());
        }

        if(_playerActions.CrouchUp)
        {
            StopCoroutine(destroyAllTethersCoroutine);
        }
    }

    //Create a tether preview line when player camera raycast hits object
    //Sets the first hit transform and position
    private void CreateTetherPreviewLine()
    {
        if (IsPlayerLookingAtCloseObject(out RaycastHit hit))
        {
            firstHitTransform = hit.transform;
            firstHitPosition =  firstHitTransform.InverseTransformPoint(hit.point);

            GameObject tetherPreview = Instantiate(tetherPreviewLinePrefab, transform.position, Quaternion.identity);
            tetherPreviewLine = tetherPreview.GetComponent<TetherPreviewLine>();

            didStartPointHit = true;
        }
        else
        {
            didStartPointHit = false;
        }
    }

    //Updates the tether preview line start point and end point
    private void UpdateTetherPreviewLine()
    {
        tetherPreviewLine.SetStartPoint(firstHitTransform.TransformPoint(firstHitPosition));

        if (IsPlayerLookingAtCloseObject(out RaycastHit hit))
        { 
            tetherPreviewLine.SetEndPoint(hit.point);
        }
        else
        {
            tetherPreviewLine.SetEndPoint(transform.position);
        }
    }

    //Creates and initializes tether parameters like hit transforms and positions
    private void CreateAndInitTether()
    {
        if (IsPlayerLookingAtCloseObject(out RaycastHit hit))
        {
            GameObject newJointTether = Instantiate(jointTetherPrefab, transform.position, Quaternion.identity);
            JointTether jointTether = newJointTether.GetComponent<JointTether>();
            
            secondHitTransform = hit.transform;
            secondHitPosition = secondHitTransform.InverseTransformPoint(hit.point);

            jointTether.Init(firstHitTransform, firstHitPosition, secondHitTransform, secondHitPosition);

            //Subscribe decrease placed tether count to when tether gets destroyed event
            jointTether.OnTetherDestroy += DecreasePlacedTetherCount;

            allTethers.Add(jointTether);

            numOfTethersPlaced++;
        }
    }

    //shoots raycast from player center screem
    private bool IsPlayerLookingAtCloseObject(out RaycastHit hit)
    {
        Ray ray = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        Physics.Raycast(ray, out hit, maxTetherStartDist, -1, QueryTriggerInteraction.Ignore);
        return hit.collider != null;
    }

    private void DeletePreviewTetherLine()
    {
        if (tetherPreviewLine != null)
        {
            Destroy(tetherPreviewLine.gameObject);
            tetherPreviewLine = null;
        }
    }

    private void DecreasePlacedTetherCount()
    {
        numOfTethersPlaced--;
    }

    IEnumerator ActivateAllTether()
    {
        yield return new WaitForSeconds(timeToActivateAllTethers);

        foreach (JointTether tether in allTethers)
        {
            tether.ActivateTether();
        }
    }

    IEnumerator DestroyAllTether()
    {
        yield return new WaitForSeconds(timeToDestroyAllTethers);

        foreach (JointTether tether in allTethers)
        {
            tether.DestroyTether();
        }

        allTethers = null;
    }
}
