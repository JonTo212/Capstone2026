using System.Collections.Generic;
using UnityEngine;

public class PlayerFade : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private float fadeStartDistance = 3f;
    [SerializeField] private float fadeEndDistance = 1f;
    [SerializeField] private float minAlpha = 0f;
    [SerializeField] private float ditherSize = 1f;

    [SerializeField] private List<Renderer> Renderers = new List<Renderer>();
    private List<Material> Materials = new List<Material>();

    public bool EnableFade { get; private set; }

    private void Awake()
    {
        if (Renderers.Count == 0)
        {
            Renderers.AddRange(GetComponentsInChildren<Renderer>());
        }
        if (Renderers.Count > 0)
        {
            foreach (Renderer renderer in Renderers)
            {
                Materials.AddRange(renderer.materials);
            }
        }
    }

    private void OnValidate()
    {
        if (Renderers.Count == 0) return;
        foreach (Renderer renderer in Renderers)
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material != null && material.HasProperty("_DitherSize"))
                {
                    material.SetFloat("_DitherSize", ditherSize);
                }
            }
        }
    }

    private void Update()
    {
        if(Materials.Count == 0) return;
        if (!EnableFade) return;

        float distance = Vector3.Distance(Camera.main.transform.position, transform.position);
        float alpha = Mathf.InverseLerp(fadeEndDistance, fadeStartDistance, distance);

        foreach (Material material in Materials)
        {
            if (material.HasProperty("_Opacity"))
            {
                material.SetFloat("_Opacity", Mathf.Lerp(minAlpha, 1f, alpha));
            }
        }
    }

    public void SetFade(bool enabled)
    {
        EnableFade = enabled;
        if (!enabled)
        {
            foreach (Material material in Materials)
            {
                if (material.HasProperty("_Opacity"))
                {
                    material.SetFloat("_Opacity", 1f);
                }
            }
        }
    }
}