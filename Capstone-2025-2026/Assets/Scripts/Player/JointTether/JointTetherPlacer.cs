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
    [SerializeField] private GameObject jointTetherPrefab;
    [SerializeField] private LayerMask tetherLayerMask;
    [SerializeField] private float maxTetherStartDist = 50f;
    [SerializeField] private int maxNumOfTethers = 3;
    [SerializeField] private int numOfTethersPlaced = 0;
    [SerializeField] private bool autoActivateTether = true;
    private bool didStartPointHit = false;
    private bool didEndPointHit = false;

    [Header("Hit Properties")]
    private Transform firstHitTransform;
    private Vector3 firstHitPosition;
    private Transform secondHitTransform;
    private Vector3 secondHitPosition;


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
            else if(_playerActions.ThrowUp)
            {
                CreateAndInitTether();
                DeletePreviewTetherLine();
            }
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

            numOfTethersPlaced++;
        }
    }

    //shoots raycast from player center screem
    private bool IsPlayerLookingAtCloseObject(out RaycastHit hit)
    {
        Ray ray = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        Physics.Raycast(ray, out hit, maxTetherStartDist, ~tetherLayerMask);
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
}
