using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;

public class JointTetherPlacer : MonoBehaviour
{
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
    public bool didStartPointHit = false;
    public bool didEndPointHit = false;

    [Header("Editable Properties")]
    [SerializeField] private float maxTetherStartDist = 50f;
    [SerializeField] private int maxNumOfTethers = 3;
    [SerializeField] private float timeToActivateAllTethers = 0.8f;
    [SerializeField] private float timeToDestroyAllTethers = 0.8f;

    [Header("Hit Properties")]
    [SerializeField] private Transform startTransform;
    [SerializeField] private Transform endTransform;
    [SerializeField] private Vector3 startLocalPosition;
    [SerializeField] private Vector3 endLocalPosition;
    [SerializeField] private TMP_Text tetherAmountText;
    [SerializeField] private TMP_Text tetherControlsText;
    public event Action OnTetherStartHit;
    public AudioManager aManage;

    #region Unity Functions
    private void Awake()
    {
        aManage = GameObject.Find("AudioManager").GetComponent<AudioManager>();
        _playerCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if(didStartPointHit)
        {
            UpdateTetherPreviewLine();
        }
    }
    #endregion

    #region Tether
    public void StartTetherPlacement()
    {
        didStartPointHit = false;
        didEndPointHit = false;

        if (numOfTethersPlaced < maxNumOfTethers)
        {
            if (GetObjectInPlayerFront(out RaycastHit hit))
            {
                CreateTetherPreviewLine();
                SetTetherStartPoint(hit.transform, hit.point);
                didStartPointHit = true;
                OnTetherStartHit?.Invoke();
            }
        }
    }

    public void StartTetherPlacement(Transform transform, Vector3 position)
    {
        if (numOfTethersPlaced < maxNumOfTethers)
        {
            CreateTetherPreviewLine();
            SetTetherStartPoint(transform, position);
            didStartPointHit = true;
            //OnTetherStartHit?.Invoke();
        }
    }

    public void EndTetherPlacement(bool autoActivate)
    {
        if (didStartPointHit)
        {
            if (GetObjectInPlayerFront(out RaycastHit hit) && hit.transform != startTransform)
            {
                didEndPointHit = true;
                Debug.Log("HIT");
                SetTetherEndPoint(hit.transform, hit.point);
                CreateAndInitTether(startTransform, startLocalPosition, endTransform, endLocalPosition, autoActivate);
            }
        }
        DeletePreviewTetherLine();
        ResetVariables();
    }

    private void SetTetherStartPoint(Transform startTransform, Vector3 startPosition)
    {
        this.startTransform = startTransform;
        startLocalPosition = startTransform.InverseTransformPoint(startPosition);

        
        aManage.PlaySFX(aManage.TetherStart, 4, 1f);
    }

    private void SetTetherEndPoint(Transform endTransform, Vector3 endPosition)
    {
        didStartPointHit = false;

        this.endTransform = endTransform;
        endLocalPosition = endTransform.InverseTransformPoint(endPosition);
        
        aManage.PlaySFX(aManage.TetherEnd, 4, 1f);
    }

    //Creates and initializes tether parameters like hit transforms and positions
    private void CreateAndInitTether(Transform startTransform, Vector3 startLocalPosition, Transform endTransform, Vector3 endLocalPosition, bool autoActivate)
    {

        GameObject newJointTether = Instantiate(jointTetherPrefab, transform.position, Quaternion.identity);
        JointTether jointTether = newJointTether.GetComponent<JointTether>();

        jointTether.Init(startTransform, startLocalPosition, endTransform, endLocalPosition, autoActivate);

        //Subscribe decrease placed tether count to when tether gets destroyed event
        jointTether.OnTetherDestroy += DecreasePlacedTetherCount;

        placedTethers.Add(jointTether);

        numOfTethersPlaced++;

        UpdateTetherAmountText();
    }
    #endregion

    #region Preview Line
    //Create a tether preview line when player camera raycast hits 
    private void CreateTetherPreviewLine()
    {
        GameObject tetherPreview = Instantiate(tetherPreviewLinePrefab, transform.position, Quaternion.identity);
        tetherPreviewLine = tetherPreview.GetComponent<TetherPreviewLine>();
    }

    //Deletes tether preview line. Called when tether placement ends
    private void DeletePreviewTetherLine()
    {
        if (tetherPreviewLine != null)
        {
            Destroy(tetherPreviewLine.gameObject);
            tetherPreviewLine = null;
        }
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
            Vector3 previewLocation = _playerCamera.transform.position + _playerCamera.transform.forward * maxTetherStartDist;
            tetherPreviewLine.SetEndPoint(previewLocation);
        }
    }
    #endregion

    #region Helper Functions
    //shoots raycast from player center screem
    private bool GetObjectInPlayerFront(out RaycastHit hit)
    {
        Ray ray = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        Physics.Raycast(ray, out hit, maxTetherStartDist, -1, QueryTriggerInteraction.Ignore);
        return hit.collider != null;
    }

    private void DecreasePlacedTetherCount(JointTether jointTether)
    {
        placedTethers.Remove(jointTether);
        numOfTethersPlaced--;
        UpdateTetherAmountText();
    }

    private void ResetVariables()
    {
        startTransform = null;
        endTransform = null;
        startLocalPosition = Vector3.zero;
        endLocalPosition = Vector3.zero;
    }

    private void UpdateTetherAmountText()
    {
        tetherAmountText.text = "Tethers: " + (maxNumOfTethers - numOfTethersPlaced).ToString() + "/" + maxNumOfTethers.ToString();
        if (numOfTethersPlaced == 0)
        {
            tetherControlsText.SetText("");
        }
        else if (numOfTethersPlaced == 1)
        {
            tetherControlsText.SetText("[E]: Activate Selected Tether\n[C]: Deactivate Selected Tether");
        }
        else if (numOfTethersPlaced > 1)
        {

            tetherControlsText.SetText("[E]: Activate Selected Tether\nHold [E]: Activate All Tethers\n[C]: Deactivate Selected Tether\nHold [C]: Deactivate all Tethers");
        }
    }
    #endregion
}
