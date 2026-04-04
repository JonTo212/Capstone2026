using System.Collections.Generic;
using UnityEngine;

public abstract class CutsceneBase : MonoBehaviour
{
    [Header("Base Settings")]
    [field: SerializeField] public float Duration { get; protected set; }
    [field: SerializeField] public float BlendInDelay { get; protected set; }
    [field: SerializeField] public float BlendInTime { get; protected set; }
    [field: SerializeField] public float BlendOutTime { get; protected set; }
    [field: SerializeField] public bool Skippable { get; protected set; } = true;
    [field: SerializeField] public bool Indefinite { get; protected set; } // needs input to proceed

    [Header("World Modifications")]
    [SerializeField] protected MonoBehaviour[] scriptsToModify;
    [SerializeField] protected GameObject[] objectsToModify;
    [SerializeField] protected GameObject[] objectsToEnableOnStart;

    public float T { get; protected set; }
    public bool IsPlaying { get; protected set; }
    public bool BlendDelayActive { get; protected set; }
    protected float Elapsed { get; private set; }
    public bool SkipInputDetected { get; private set; }


    public virtual void OnCutscenePrepare() { }

    public virtual void OnCutsceneStart()
    {
        Elapsed = 0f;
        T = 0f;
        IsPlaying = true;
        SkipInputDetected = false;
    }

    public virtual void OnCutsceneTick()
    {
        Elapsed += Time.fixedDeltaTime;

        if (Indefinite)
        {
            T = Elapsed;
        }
        else
        {
            T = Mathf.Clamp01(Elapsed / Duration);
        }
    }

    public virtual void OnBlendTick(float t) { }
    public virtual void OnBlendOutTick(float t) { }
    public virtual void OnCutsceneSkip() { }
    public virtual void OnCutsceneEnd() => IsPlaying = false;

    public virtual void OnCutsceneUpdate() { }
    public virtual void OnCutsceneLateUpdate() { }
    public void SetBlendDelayActive(bool active) => BlendDelayActive = active;
    public void EndIndefiniteCutscene () => SkipInputDetected = true;
    protected void HandleScripts(bool active)
    {
        foreach (var script in scriptsToModify)
            if (script != null) script.enabled = active;

        foreach (var obj in objectsToModify)
            if (obj != null) obj.SetActive(active);
    }

    #region Spline Utility
    protected Vector3 GetSplinePoint(List<Transform> nodes, float t)
    {
        if (nodes == null || nodes.Count < 2) return Vector3.zero;
        if (nodes.Count == 2) return Vector3.Lerp(nodes[0].position, nodes[1].position, t);

        int numSections = nodes.Count - 1;
        int currPt = Mathf.Min(Mathf.FloorToInt(t * numSections), numSections - 1);
        float u = t * numSections - currPt;

        Vector3 p0 = nodes[Mathf.Max(currPt - 1, 0)].position;
        Vector3 p1 = nodes[currPt].position;
        Vector3 p2 = nodes[currPt + 1].position;
        Vector3 p3 = nodes[Mathf.Min(currPt + 2, nodes.Count - 1)].position;

        return CatmullRom(p0, p1, p2, p3, u);
    }

    private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }
    #endregion
}