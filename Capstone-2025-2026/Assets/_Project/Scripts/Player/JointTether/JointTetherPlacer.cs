using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;
using static CharacterSkinController;
using DG.Tweening;

public class JointTetherPlacer : MonoBehaviour
{
    private Camera _playerCamera;

    [Header("Tether Preview Line Properties")]
    [SerializeField] private GameObject tetherPreviewLinePrefab;
    private TetherPreviewLine tetherPreviewLine;

    [Header("Properties")]
    [SerializeField] private GameObject jointTetherPrefab;
    [SerializeField] private LayerMask tetherLayerMask;
    [SerializeField] private Material[] untetherableMaterials;
    [SerializeField] private int numOfTethersPlaced = 0;
    [SerializeField] public bool autoActivateTether = true;
    public List<JointTether> placedTethers { get; private set; } = new List<JointTether>();
    public bool didStartPointHit = false;
    public bool didEndPointHit = false;
    public bool isStartPointValid = false;

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
    public float originalAmountTextPosition;
    public event Action OnTetherStartHit;
    public AudioManager aManage;

    #region Unity Functions
    private void Awake()
    {
        aManage = GameObject.Find("AudioManager").GetComponent<AudioManager>();
        _playerCamera = Camera.main;
        originalAmountTextPosition = tetherAmountText.gameObject.transform.position.y;
    }

    private void Start()
    {
        GameObject tetherPreview = Instantiate(tetherPreviewLinePrefab, transform.position, Quaternion.identity);
        tetherPreviewLine = tetherPreview.GetComponent<TetherPreviewLine>();
        Invoke(nameof(TextOffScreen),5f);
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
                isStartPointValid = true;
            }
            if(IsTetherPointValid(hit.transform) == false)
            {
                isStartPointValid = false;
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
                SetTetherEndPoint(hit.transform, hit.point);

                if(autoActivateTether)
                {
                    autoActivate = true;
                }

                if(IsTetherPointValid(hit.transform) && isStartPointValid)
                {
                    CreateAndInitTether(startTransform, startLocalPosition, endTransform, endLocalPosition, autoActivate);
                }
            }
        }
        DeletePreviewTetherLine();
        ResetVariables();
    }

    private void SetTetherStartPoint(Transform startTransform, Vector3 startPosition)
    {
        this.startTransform = startTransform;

        if(startTransform.gameObject.TryGetComponent<Prop>(out Prop propComponent))
        {

            if (propComponent.CheckNearestGrabPoint(startPosition) != null)
            {
                startPosition = propComponent.CheckNearestGrabPoint(startPosition).position;
            }
        }
        startLocalPosition = startTransform.InverseTransformPoint(startPosition);

        aManage.PlaySFX(aManage.TetherStart, 4, 1f);
    }

    private void SetTetherEndPoint(Transform endTransform, Vector3 endPosition)
    {
        didStartPointHit = false;

        this.endTransform = endTransform;

        if (endTransform.gameObject.TryGetComponent<Prop>(out Prop propComponent))
        {
            if (propComponent.CheckNearestGrabPoint(endPosition) != null)
            {
                endPosition = propComponent.CheckNearestGrabPoint(endPosition).position;
            }
        }

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

        jointTether.playerTransform = transform;

        numOfTethersPlaced++;

        UpdateTetherAmountText();
    }
    #endregion

    #region Preview Line
    //Create a tether preview line when player camera raycast hits 
    private void CreateTetherPreviewLine()
    {
        //GameObject tetherPreview = Instantiate(tetherPreviewLinePrefab, transform.position, Quaternion.identity);
        //tetherPreviewLine = tetherPreview.GetComponent<TetherPreviewLine>();
        tetherPreviewLine.gameObject.SetActive(true);
    }

    //Deletes tether preview line. Called when tether placement ends
    private void DeletePreviewTetherLine()
    {
        //if (tetherPreviewLine != null)
        //{
        //    Destroy(tetherPreviewLine.gameObject);
        //    tetherPreviewLine = null;
        //}

        tetherPreviewLine.gameObject.SetActive(false);
    }

    //Updates the tether preview line start point and end point
    private void UpdateTetherPreviewLine()
    {
        tetherPreviewLine.SetStartPoint(startTransform.TransformPoint(startLocalPosition));

        if (GetObjectInPlayerFront(out RaycastHit hit))
        {
            Vector3 endPosition = hit.point;

            if (hit.transform.TryGetComponent<Prop>(out Prop propComponent))
            {
                if (propComponent.CheckNearestGrabPoint(endPosition) != null)
                {
                    endPosition = propComponent.CheckNearestGrabPoint(endPosition).position;
                }
            }

            tetherPreviewLine.SetEndPoint(endPosition);

            if (IsTetherPointValid(hit.transform) && isStartPointValid)
            {
                Debug.Log("valid");
                tetherPreviewLine.SetColorToValid();
            }
            else
            {
                
                tetherPreviewLine.SetColorToInvalid();
            }
        }
        else
        {
            tetherPreviewLine.SetColorToInvalid();
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

    public void TextOnScreen()
    {
        tetherAmountText.gameObject.transform.DOMoveY(originalAmountTextPosition, 2);
    }

    public void TextOffScreen()
    {
        tetherAmountText.gameObject.transform.DOMoveY(-211,2);
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
        didStartPointHit = false;
        didEndPointHit = false;
        startLocalPosition = Vector3.zero;
        endLocalPosition = Vector3.zero;
    }

    private void UpdateTetherAmountText()
    {
        tetherAmountText.text = "Tethers: " + (maxNumOfTethers - numOfTethersPlaced).ToString() + "/" + maxNumOfTethers.ToString();
        if (numOfTethersPlaced == 0)
        {
            tetherControlsText.SetText("");
            TextOffScreen();
        }
        else if (numOfTethersPlaced == 1)
        {
            tetherControlsText.SetText("[E]: Activate Selected Tether\n[C]: Deactivate Selected Tether");
            TextOnScreen();
        }
        else if (numOfTethersPlaced > 1)
        {

            tetherControlsText.SetText("[E]: Activate Selected Tether\nHold [E]: Activate All Tethers\n[C]: Deactivate Selected Tether\nHold [C]: Deactivate all Tethers");
        }
    }

    //TODO: Make this function only run when a new object is detected
    private bool IsTetherPointValid(Transform transform)
    {
        if(transform.TryGetComponent<Renderer>(out Renderer renderer))
        {
            foreach(Material objectMat in renderer.sharedMaterials)
            {
                foreach(Material untetherableMat in untetherableMaterials)
                {
                    if(objectMat == untetherableMat)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }
    #endregion

    #region Dev Functions
    public void ToggleAutoActivateTether()
    {
        autoActivateTether = !autoActivateTether;
    }

    #endregion
}
