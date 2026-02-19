using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFadeData : MonoBehaviour
{
    public List<Renderer> Renderers = new List<Renderer>();
    public List<Material> Materials = new List<Material>();
    [HideInInspector]
    public float InitialAlpha;

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

        InitialAlpha = Materials[0].color.a;
    }
}