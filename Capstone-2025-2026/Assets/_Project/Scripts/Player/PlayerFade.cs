using System.Collections.Generic;
using UnityEngine;

public class PlayerFade : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private float fadeStartDistance = 3f;
    [SerializeField] private float fadeEndDistance = 1f;
    [SerializeField] private float minAlpha = 0f;

    [SerializeField] private List<Renderer> Renderers = new List<Renderer>();
    private List<Material> Materials = new List<Material>();

    private void Awake()
    {
        if (Renderers.Count == 0)
        {
            Renderers.AddRange(GetComponentsInChildren<Renderer>());
        }
        foreach (Renderer renderer in Renderers)
        {
            Materials.AddRange(renderer.materials);
        }
    }

    private void Update()
    {
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
}