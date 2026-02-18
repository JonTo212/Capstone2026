using DG.Tweening;
using FMODUnity;
using NUnit.Framework.Constraints;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;
using static CharacterSkinController;

public class JointTetherPlacer : MonoBehaviour
{
    private Camera _playerCamera;

    [Header("Tether Preview Line Properties")]
    [SerializeField] private GameObject tetherPreviewLinePrefab;
    private TetherPreviewLine tetherPreviewLine;

    [Header("Properties")]
    [SerializeField] private GameObject jointTetherPrefab;
    [SerializeField] private GameObject jointTether;
    [SerializeField] private Transform tetherRangeSphere;
    [SerializeField] private LayerMask tetherLayerMask;
    [SerializeField] private LayerMask layersToIgnore;
    [SerializeField] private Material[] untetherableMaterials;
    [SerializeField] private int numOfTethersPlaced = 0;
    [SerializeField] public bool autoActivateTether = true;
    public List<JointTether> placedTethers { get; private set; } = new List<JointTether>();
    public bool didStartPointHit = false;
    public bool didEndPointHit = false;
    public bool isStartPointValid = true;
    private bool isTetherPlacementValid = true;
    private Transform previousSelectedTransform;

    [Header("Tether mode Properties")]
    private bool isTetherModeActive = false;
    private Prop currentHeldProp;
    private float defaultTimeDeltaTime;
    private List<JointTether> placedTethersDuringTetherMode = new List<JointTether>();

    [Header("Editable Properties")]
    [SerializeField] private float maxTetherStartDist = 50f;
    [SerializeField] private float cameraMaxDistance = 0f;
    [SerializeField] private float maxTetherLength = 20f;
    [SerializeField] private int maxNumOfTethers = 3;
    [SerializeField] private float timeToActivateAllTethers = 0.8f;
    [SerializeField] private float timeToDestroyAllTethers = 0.8f;

    [Header("Hit Properties")]
    [SerializeField] private Transform startTransform;
    [SerializeField] private Transform endTransform;
    [SerializeField] private Vector3 startWorldLocation;
    [SerializeField] private Vector3 endWorldLocation;
    [SerializeField] private Vector3 startLocalPosition;
    [SerializeField] private Vector3 endLocalPosition;
    [SerializeField] private TMP_Text tetherAmountText;
    [SerializeField] private TMP_Text tetherControlsText;
    public float originalAmountTextPosition;
    public event Action<bool> OnPlacementValidityUpdate;
    public event Action OnTetherStartHit;
    
    public float MaxTetherStartRange => maxTetherStartDist;

    #region Unity Functions
    private void Awake()
    {
        _playerCamera = Camera.main;
        originalAmountTextPosition = tetherAmountText.gameObject.transform.position.y;
    }

    private void Start()
    {
        GameObject tetherPreview = Instantiate(tetherPreviewLinePrefab, transform.position, Quaternion.identity);
        tetherPreviewLine = tetherPreview.GetComponent<TetherPreviewLine>();
        Invoke(nameof(TextOffScreen),5f);
        tetherRangeSphere.localScale = Vector3.one * maxTetherLength * 2;
        tetherRangeSphere.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (!isTetherModeActive)
        {
            if (didStartPointHit)
            {
                UpdateTetherPreviewLine();
                tetherRangeSphere.transform.position = startTransform.TransformPoint(startLocalPosition);
            }
        }

        GetObjectInPlayerFront(out RaycastHit hit);

        if (hit.transform != null)
        {
            if (previousSelectedTransform != hit.transform)
            {
                bool isValid = IsTetherPointValid(hit.transform);

                if (isTetherPlacementValid != isValid)
                {
                    isTetherPlacementValid = isValid;
                    OnPlacementValidityUpdate.Invoke(isValid);
                }
            }
            previousSelectedTransform = hit.transform;

        }
        else
        {
            if (didStartPointHit)
            {
                if (isTetherPlacementValid != false)
                {
                    isTetherPlacementValid = false;
                    OnPlacementValidityUpdate.Invoke(false);
                }
            }
            else
            {
                isTetherPlacementValid = true;
                OnPlacementValidityUpdate.Invoke(true);
            }

            previousSelectedTransform = null;
        }

    }
    #endregion

    #region Tether
    public void StartTetherPlacement()
    {
        didStartPointHit = false;
        didEndPointHit = false;

        if (GetObjectInPlayerFront(out RaycastHit hit))
        {
            if (Vector3.Distance(transform.position, hit.point) > maxTetherStartDist) return;
            CreateTetherPreviewLine();
            SetTetherStartPoint(hit.transform, hit.point);
            startWorldLocation = hit.point;
            didStartPointHit = true;
            OnTetherStartHit?.Invoke();
            isStartPointValid = true;
            tetherRangeSphere.gameObject.SetActive(true);
        }
        if(IsTetherPointValid(hit.transform) == false)
        {
            isStartPointValid = false;
        }
    }

    public void StartTetherPlacement(Transform transform, Vector3 position)
    {
        isStartPointValid = true;
        CreateTetherPreviewLine();
        startWorldLocation = position;
        SetTetherStartPoint(transform, position);
        didStartPointHit = true;
        tetherRangeSphere.gameObject.SetActive(true);
        tetherRangeSphere.position = position;
        //OnTetherStartHit?.Invoke();
    }

    public void EndTetherPlacement(bool autoActivate)
    {
        if (didStartPointHit)
        {
            if (GetObjectInPlayerFront(out RaycastHit hit) && hit.transform != startTransform && Vector3.Distance(startWorldLocation, hit.point) < maxTetherLength)
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

                    if (numOfTethersPlaced > maxNumOfTethers)
                    {
                        placedTethers[0].DestroyTether();
                    }
                }
            }
        }
        DeletePreviewTetherLine();
        ResetVariables();
        tetherRangeSphere.gameObject.SetActive(false);
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

        //AudioManager.Instance.PlaySFX(AudioManager.Instance.TetherStart, 4, 1f);
        RuntimeManager.PlayOneShot("event:/TetherStart", transform.position);
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



        RuntimeManager.PlayOneShot("event:/TetherEnd", transform.position);
    }

    //Creates and initializes tether parameters like hit transforms and positions
    private JointTether CreateAndInitTether(Transform startTransform, Vector3 startLocalPosition, Transform endTransform, Vector3 endLocalPosition, bool autoActivate)
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

        return jointTether;
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
            endWorldLocation = hit.point;

            if (hit.transform.TryGetComponent<Prop>(out Prop propComponent))
            {
                if (propComponent.CheckNearestGrabPoint(endPosition) != null)
                {
                    endPosition = propComponent.CheckNearestGrabPoint(endPosition).position;
                }
            }

            tetherPreviewLine.SetEndPoint(endPosition);

            if (IsTetherPointValid(hit.transform) && isStartPointValid && Vector3.Distance(startWorldLocation, hit.point) < maxTetherLength)
            {
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

    #region TetherMode

    public void EnterTetherMode(Prop currentHeldProp)
    {
        this.currentHeldProp = currentHeldProp;
        CreateTetherPreviewLine();
    }

    public void HandleTetherMode()
    {
        if (GetObjectInPlayerFront(out RaycastHit hit) && hit.transform != startTransform)
        {
            if (hit.transform == currentHeldProp)
            {

            }
            else
            {
                UpdateTetherPreviewTetherMode(GetClosestAttachmentPoint(currentHeldProp, hit.point));
            }
        }
    }

    public void TetherModeStartTetherPlacement()
    {
        if (GetObjectInPlayerFront(out RaycastHit hit))
        {
            SetTetherStartPoint(currentHeldProp.transform, GetClosestAttachmentPoint(currentHeldProp, hit.point).position);
            didStartPointHit = true;
        }
    }

    public void TetherModeEndTetherPlacement()
    {
        if (didStartPointHit)
        {
            if (GetObjectInPlayerFront(out RaycastHit hit) && hit.transform != startTransform)
            {
                SetTetherEndPoint(hit.transform, hit.point);

                JointTether tether = CreateAndInitTether(startTransform, startLocalPosition, endTransform, endLocalPosition, false);
                placedTethersDuringTetherMode.Add(tether);
            }
        }

        ResetVariables();
    }

    public void TetherModeQuickPlaceTether()
    {
        if (GetObjectInPlayerFront(out RaycastHit hit) && hit.transform != startTransform)
        {
            SetTetherStartPoint(currentHeldProp.transform, GetClosestAttachmentPoint(currentHeldProp, hit.point).position);
            SetTetherEndPoint(hit.transform, hit.point);

            CreateAndInitTether(startTransform, startLocalPosition, endTransform, endLocalPosition, false);
        }

        if (numOfTethersPlaced > maxNumOfTethers)
        {
            placedTethers[0].DestroyTether();
        }

        ResetVariables();
    }

    public void ExitTetherMode()
    {
        foreach (JointTether tether in placedTethersDuringTetherMode)
        {
            tether.ActivateTether();
        }

        DeletePreviewTetherLine();
        ResetVariables();

        placedTethersDuringTetherMode.Clear();
    }

    private Transform GetClosestAttachmentPoint(Prop prop, Vector3 target)
    {
        if (prop.GrabPoints.Count < 1)
        {
            return prop.transform;
        }

        Transform[] points = prop.GrabPoints.ToArray();

        Transform closestPoint = points[0];

        for (int i = 0; i < points.Length; i++)
        {
            if (Vector3.Distance(points[i].position, target) < Vector3.Distance(closestPoint.position, target))
            {
                closestPoint = points[i];
            }
        }

        return closestPoint;
    }

    private void UpdateTetherPreviewTetherMode(Transform grabPoint)
    {
        if (startTransform == null)
        {
            tetherPreviewLine.SetStartPoint(grabPoint.position);
        }
        else
        {
            tetherPreviewLine.SetStartPoint(startTransform.TransformPoint(startLocalPosition));
        }

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
        Physics.Raycast(ray, out hit, 500f, ~layersToIgnore, QueryTriggerInteraction.Ignore);

        Vector3 hitToPlayer = transform.position - hit.point;
        Vector3 hitToCamera = _playerCamera.transform.position - hit.point;
        Vector3 projectedPlayerToCameraHit = Vector3.Project(hitToPlayer, hitToCamera);

        Vector3 cameraToHit = hit.point - _playerCamera.transform.position;
        Vector3 cameraToPlayer = transform.position - _playerCamera.transform.position;
        Vector3 projectedCameraToPlayer = Vector3.Project(cameraToPlayer, cameraToHit);

        cameraMaxDistance = projectedCameraToPlayer.magnitude + projectedPlayerToCameraHit.magnitude;
       

        hit = new RaycastHit();

        Physics.Raycast(ray, out hit, cameraMaxDistance, ~layersToIgnore, QueryTriggerInteraction.Ignore);
        //float cameraPlayerAngle = Mathf.Acos(Vector3.Dot(hitToPlayer, hitToCamera) / (hitToPlayer.magnitude * hitToPlayer.magnitude));

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
        startLocalPosition = Vector3.zero;
        endLocalPosition = Vector3.zero;

        Invoke(nameof(ResetEndPointHit), 0.25f);
    }

    private void ResetEndPointHit()
    {
        //for animation
        didEndPointHit = false;
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
