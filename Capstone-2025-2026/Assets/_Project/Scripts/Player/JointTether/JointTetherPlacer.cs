using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static CharacterSkinController;

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
    [SerializeField] public bool autoActivateTether = true;
    public List<JointTether> placedTethers { get; private set; } = new List<JointTether>();
    public bool didStartPointHit = false;
    public bool didEndPointHit = false;

    [Header("Tether Mode Properties")]
    [SerializeField] private float slowdownFactor = 0.2f;
    [SerializeField] private float slowDownDurationPerTether = 4f;
    [SerializeField] CinemachineInputAxisController CNInputAxisController;
    private bool isTetherModeActive = false;
    private Prop currentHeldProp;

    [Header("Tether Mode Post Process properties")]
    [SerializeField] private Volume postProcessVolume;
    DepthOfField tetherModeDepthOfField;
    ChromaticAberration tetherModeChromaticAberation;
    PaniniProjection tetherModePaniniProjection;

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

        postProcessVolume.profile.TryGet(out tetherModeDepthOfField);
        postProcessVolume.profile.TryGet(out tetherModeChromaticAberation);
        postProcessVolume.profile.TryGet(out tetherModePaniniProjection);
    }

    private void Start()
    {
        tetherModeChromaticAberation.active = false;
        tetherModeDepthOfField.active = false;
        tetherModePaniniProjection.active = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (isTetherModeActive)
        {
           
        }
        else
        {
            if(didStartPointHit)
            {
                UpdateTetherPreviewLine();
            }
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
                SetTetherEndPoint(hit.transform, hit.point);

                if(autoActivateTether)
                {
                    autoActivate = true;
                }

                CreateAndInitTether(startTransform, startLocalPosition, endTransform, endLocalPosition, autoActivate);
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

    #region TetherMode

    public void EnterSlowTetherMode(Prop currentHeldProp)
    {
        Time.timeScale = slowdownFactor;
        this.currentHeldProp = currentHeldProp;
        tetherModeChromaticAberation.active = true;
        tetherModeDepthOfField.active = true;
        tetherModePaniniProjection.active = true;
        CreateTetherPreviewLine();
        Camera.main.GetComponent<CinemachineBrain>().UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
        Camera.main.GetComponent<CinemachineBrain>().IgnoreTimeScale = true;

        foreach (var c in CNInputAxisController.Controllers)
        {
            if (c.Name == "Look Orbit X")
            {
                c.Input.Gain *= (1f/slowdownFactor);
                c.Input.Gain *= (1f / 3f);
                c.Driver.AccelTime *= slowdownFactor;
                c.Driver.DecelTime *= slowdownFactor;
                continue;
            }

            if (c.Name == "Look Orbit Y")
            {
                c.Input.Gain *= (1f / slowdownFactor);
                c.Input.Gain *= (1f / 3f);
                c.Driver.AccelTime *= slowdownFactor;
                c.Driver.DecelTime *= slowdownFactor;
                continue;
            }
        }
    }

    public void HandleTetherMode()
    {
        Time.fixedDeltaTime = Time.timeScale * 0.02f;
        if (GetObjectInPlayerFront(out RaycastHit hit) && hit.transform != startTransform)
        {
            if(hit.transform == currentHeldProp)
            {

            }
            else
            {
                UpdateTetherPreviewTetherMode(GetClosestAttachmentPoint(currentHeldProp, hit.point));
            }
        }
    }

    public void TetherModeQuickPlaceTether()
    {
        if (numOfTethersPlaced < maxNumOfTethers)
        {
            if (GetObjectInPlayerFront(out RaycastHit hit) && hit.transform != startTransform)
            {
                SetTetherStartPoint(currentHeldProp.transform, GetClosestAttachmentPoint(currentHeldProp, hit.point).position);
                SetTetherEndPoint(hit.transform, hit.point);

                CreateAndInitTether(startTransform, startLocalPosition, endTransform, endLocalPosition, true);
            }
        }

        ResetVariables();
    }

    public void ExitTetherMode()
    {
        Time.timeScale = 1f;
        tetherModeChromaticAberation.active = false;
        tetherModeDepthOfField.active = false;
        tetherModePaniniProjection.active = false;
        DeletePreviewTetherLine();
        ResetVariables();
        CinemachineImpulseManager.Instance.IgnoreTimeScale = true;
        Camera.main.GetComponent<CinemachineBrain>().UpdateMethod = CinemachineBrain.UpdateMethods.SmartUpdate;
        Camera.main.GetComponent<CinemachineBrain>().IgnoreTimeScale = false;

        foreach(var c in CNInputAxisController.Controllers)
        {
            if (c.Name == "Look Orbit X")
            {
                c.Input.Gain *= slowdownFactor;
                c.Input.Gain *= 3f;
                c.Driver.AccelTime *= (1f/slowdownFactor);
                c.Driver.DecelTime *= (1f/slowdownFactor);
                continue;
            }

            if (c.Name == "Look Orbit Y")
            {
                c.Input.Gain *= slowdownFactor;
                c.Input.Gain *= 3f;
                c.Driver.AccelTime *= (1f/slowdownFactor);
                c.Driver.DecelTime *= (1f/slowdownFactor);
                continue;
            }
        }
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
        tetherPreviewLine.SetStartPoint(grabPoint.position);

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

    #region Dev Functions
    public void ToggleAutoActivateTether()
    {
        autoActivateTether = !autoActivateTether;
    }

    #endregion
}
