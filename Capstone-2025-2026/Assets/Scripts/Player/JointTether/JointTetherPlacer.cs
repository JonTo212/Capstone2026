using NodeCanvas.Tasks.Actions;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
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
    [SerializeField] private float maxTetherStartDist = 50f;
    [SerializeField] private bool autoActivateTether = true;
    [SerializeField] private LayerMask tetherLayerMask;

    private Transform firstHitTransform;
    private Vector3 firstHitPosition;
    private Transform secondHitTransform;
    private Vector3 secondHitPosition;

    [SerializeField] private int maxNumOfTethers = 3;
    [SerializeField] private int numOfTethersPlaced = 0;
    private bool didStartPointHit = false;
    private bool didEndPointHit = false;

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
            if(_playerActions.ThrowHeld && didStartPointHit)
            {
                UpdateTetherPreviewLine();
            }
            if(_playerActions.ThrowUp)
            {
                CreateAndSetTether();
                DeletePreviewTetherLine();
            }
        }
    }

    private void CreateTetherPreviewLine()
    {
        RaycastHit hit;
        Ray ray = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if (Physics.Raycast(ray, out hit, maxTetherStartDist, ~tetherLayerMask))
        {
            firstHitTransform = hit.transform;
            firstHitPosition =  firstHitTransform.InverseTransformPoint(hit.point);

            GameObject tetherPreview = Instantiate(tetherPreviewLinePrefab, transform.position, Quaternion.identity);
            tetherPreviewLine = tetherPreview.GetComponent<TetherPreviewLine>();

            didStartPointHit = true;
        }
    }

    private void UpdateTetherPreviewLine()
    {
        tetherPreviewLine.SetStartPoint(firstHitTransform.TransformPoint(firstHitPosition));

        RaycastHit hit;
        Ray ray = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out hit, maxTetherStartDist, ~tetherLayerMask))
        {
            tetherPreviewLine.SetEndPoint(hit.point);
        }
        else
        {
            tetherPreviewLine.SetEndPoint(transform.position);
        }
    }

    private void CreateAndSetTether()
    {
        RaycastHit hit;
        Ray ray = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if (Physics.Raycast(ray, out hit, maxTetherStartDist, ~tetherLayerMask))
        {
            GameObject newJointTether = new GameObject("JointTether");
            JointTether jointTether = newJointTether.AddComponent<JointTether>();
            
            secondHitTransform = hit.transform;
            secondHitPosition = secondHitTransform.InverseTransformPoint(hit.point);

            jointTether.Init(firstHitTransform, firstHitPosition, secondHitTransform, secondHitPosition);
        }
    }

    private void DeletePreviewTetherLine()
    {
        if (tetherPreviewLine != null)
        {
            Destroy(tetherPreviewLine.gameObject);
            tetherPreviewLine = null;
        }
    }
}
