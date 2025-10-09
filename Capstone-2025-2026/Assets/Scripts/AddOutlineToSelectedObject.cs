using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AddOutlineToSelectedObject : MonoBehaviour
{
    [SerializeField] private Material outlineShader;
    private Camera playerCamera;
    public GameObject currentSelectedObject;
    public List<Material> selectedObjectOriginalMaterials;
    public List<Material> selectedObjectNewMaterials;
    public LayerMask playerLayer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if(Physics.Raycast(ray, out RaycastHit hit, 50f))
        {
            if(hit.transform.gameObject == currentSelectedObject)
            {
                return;
            }
            else
            {
                if(currentSelectedObject != null && currentSelectedObject.GetComponent<MeshRenderer>() != null)
                {
                    currentSelectedObject.GetComponent<MeshRenderer>().materials = selectedObjectOriginalMaterials.ToArray();
                }

                Debug.Log("hit object");
                currentSelectedObject = hit.transform.gameObject;
                if(currentSelectedObject.GetComponent<MeshRenderer>() != null)
                {
                    Material[] materials = currentSelectedObject.GetComponent<MeshRenderer>().materials;

                    selectedObjectOriginalMaterials = new List<Material>();
                    
                    foreach(Material mat in materials)
                    {
                        selectedObjectOriginalMaterials.Add(mat);
                    }

                    selectedObjectNewMaterials = selectedObjectNewMaterials = new List<Material>(selectedObjectOriginalMaterials);
                    selectedObjectNewMaterials.Add(outlineShader);
                    currentSelectedObject.GetComponent<MeshRenderer>().materials = selectedObjectNewMaterials.ToArray();
                }
            }
        }
        else
        {
            if(currentSelectedObject != null && currentSelectedObject.GetComponent<MeshRenderer>() != null)
            {
                currentSelectedObject.GetComponent<MeshRenderer>().materials = selectedObjectOriginalMaterials.ToArray();
                selectedObjectNewMaterials = null;
                selectedObjectOriginalMaterials = null;
                currentSelectedObject = null;
            }
        }
    }
}
