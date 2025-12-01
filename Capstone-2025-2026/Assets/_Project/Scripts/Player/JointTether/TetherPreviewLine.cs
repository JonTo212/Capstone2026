using Unity.VisualScripting;
using UnityEngine;

public class TetherPreviewLine : MonoBehaviour
{
    [Header("Components")]
    private LineRenderer lineRenderer;
    [SerializeField] private Transform startPointVisuals;
    [SerializeField] private Transform endPointVisuals;
    public Material[] tetherPreviewMaterials;

    [Header("Variables")]
    [ColorUsage(true, true)][SerializeField] private Color validColor = Color.yellow;
    [ColorUsage(true, true)][SerializeField] private Color invalidColor = Color.red;
    [SerializeField] private LayerMask tetherLayerMask;
    private Vector3 startPoint;
    private Vector3 endPoint;
    private bool isValidPlacement = true;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Start()
    {
        Debug.Log("Start run");
        tetherPreviewMaterials = new Material[5];
        tetherPreviewMaterials[0] = lineRenderer.material;
        tetherPreviewMaterials[1] = startPointVisuals.GetChild(0).GetComponent<Renderer>().material;
        tetherPreviewMaterials[2] = startPointVisuals.GetChild(1).GetComponent<Renderer>().material;
        tetherPreviewMaterials[3] = endPointVisuals.GetChild(0).GetComponent<Renderer>().material;
        tetherPreviewMaterials[4] = endPointVisuals.GetChild(1).GetComponent<Renderer>().material;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        //CheckIfSomethingIsBlocking();
    }

    public void SetStartPoint(Vector3 newPos)
    {
        startPoint = newPos;
        startPointVisuals.position = newPos;
        lineRenderer.SetPosition(0, newPos);
    }

    public void SetEndPoint(Vector3 newPos)
    {
        endPoint = newPos;
        endPointVisuals.position = newPos;
        lineRenderer.SetPosition(1, newPos);
    }

    public void SetColorToInvalid()
    {

        if (isValidPlacement == true)
        {
            foreach(Material mat in tetherPreviewMaterials)
            {
                mat.SetColor("_Emissive", invalidColor);
            }
            isValidPlacement = false;
        }
    }

    public void SetColorToValid()
    {
        if (isValidPlacement == false)
        {
            foreach (Material mat in tetherPreviewMaterials)
            {
                mat.SetColor("_Emissive", validColor);
            }
            isValidPlacement = true;
        }
    }

    private void CheckIfSomethingIsBlocking()
    {
        RaycastHit hit;

        Vector3 startEndDirection = (endPoint - startPoint).normalized;
        float startEndDistance = (endPoint - startPoint).magnitude;

        if (Physics.Raycast(startPoint, startEndDirection, out hit, startEndDistance, ~tetherLayerMask, QueryTriggerInteraction.Ignore))
        {
            lineRenderer.startColor = invalidColor;
            lineRenderer.endColor = invalidColor;
        }
        else
        {
            lineRenderer.startColor = validColor;
            lineRenderer.endColor = validColor;
        }
    }
}