using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Unity.VisualScripting;
using System.Security.Cryptography;

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
    public List<JointTether> placedTethers { get; private set; } = new List<JointTether>();
    private bool didStartPointHit = false;
    private bool didEndPointHit = false;

    [Header("Editable Properties")]
    [SerializeField] private float maxTetherStartDist = 50f;
    [SerializeField] private int maxNumOfTethers = 3;
    [SerializeField] private float timeToActivateAllTethers = 0.8f;
    [SerializeField] private float timeToDestroyAllTethers = 0.8f;

    [Header("Hit Properties")]
    private Transform startTransform;
    private Transform endTransform;
    private Vector3 startLocalPosition;
    private Vector3 endLocalPosition;

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
        if(_playerActions.AltDown)
        {
            StartTetherPlacement();
        }
        else if(_playerActions.AltUp && didStartPointHit)
        {
            EndTetherPlacement();
        }

        if(didStartPointHit)
        {
            UpdateTetherPreviewLine();
        }
    }

    public void StartTetherPlacement()
    {
        if(numOfTethersPlaced < maxNumOfTethers)
        {
            if (GetObjectInPlayerFront(out RaycastHit hit))
            {
                CreateTetherPreviewLine();
                SetTetherStartPoint(hit.transform, hit.point);
                didStartPointHit = true;
            }
        }
    }

    public void EndTetherPlacement()
    {
        if(didStartPointHit)
        {
            if (GetObjectInPlayerFront(out RaycastHit hit))
            {
                SetTetherEndPoint(hit.transform, hit.point);
                CreateAndInitTether(startTransform, startLocalPosition, endTransform, endLocalPosition);
                DeletePreviewTetherLine();
            }
        }

        ResetVariables();
    }

    //Create a tether preview line when player camera raycast hits 
    private void CreateTetherPreviewLine()
    {
        GameObject tetherPreview = Instantiate(tetherPreviewLinePrefab, transform.position, Quaternion.identity);
        tetherPreviewLine = tetherPreview.GetComponent<TetherPreviewLine>();
    }

    private void SetTetherStartPoint(Transform startTransform, Vector3 startPosition)
    {
        this.startTransform = startTransform;
        startLocalPosition = startTransform.InverseTransformPoint(startPosition);
    }

    private void SetTetherEndPoint(Transform endTransform, Vector3 endPosition)
    {
        this.endTransform = endTransform;
        endLocalPosition = endTransform.InverseTransformPoint(endPosition);
    }

    //Updates the tether preview line start point and end point
    private void UpdateTetherPreviewLine()
    {
        tetherPreviewLine.SetStartPoint(startTransform.TransformPoint(startLocalPosition));

        if (GetObjectInPlayerFront(out RaycastHit hit))
        { 
            tetherPreviewLine.SetEndPoint(hit.point);
        }
        else
        {
            tetherPreviewLine.SetEndPoint(transform.position);
        }
    }

    //Creates and initializes tether parameters like hit transforms and positions
    private void CreateAndInitTether(Transform startTransform, Vector3 startLocalPosition, Transform endTransform, Vector3 endLocalPosition)
    {
        GameObject newJointTether = Instantiate(jointTetherPrefab, transform.position, Quaternion.identity);
        JointTether jointTether = newJointTether.GetComponent<JointTether>();

        jointTether.Init(startTransform, startLocalPosition, endTransform, endLocalPosition);

        //Subscribe decrease placed tether count to when tether gets destroyed event
        jointTether.OnTetherDestroy += DecreasePlacedTetherCount;

        placedTethers.Add(jointTether);

        numOfTethersPlaced++;
    }

    //shoots raycast from player center screem
    private bool GetObjectInPlayerFront(out RaycastHit hit)
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

    private void ResetVariables()
    {
        startTransform = null;
        endTransform = null;
        startLocalPosition = Vector3.zero;
        endLocalPosition = Vector3.zero;

        didStartPointHit = false;
    }
}
