using UnityEngine;

public abstract class CutsceneBase : MonoBehaviour
{
    [field: SerializeField] public float Duration { get; protected set; }
    [field: SerializeField] public float BlendInDelay { get; protected set; }
    [field: SerializeField] public float BlendInTime { get; protected set; }
    [field: SerializeField] public float BlendOutTime { get; protected set; }
    [field: SerializeField] public bool Skippable { get; protected set; } = true;

    public float T { get; protected set; }
    public bool IsPlaying { get; protected set; }
    public bool BlendDelayActive { get; protected set; }
    protected float Elapsed { get; private set; }

    public virtual void OnCutscenePrepare() { }

    public virtual void OnCutsceneStart()
    {
        Elapsed = 0f;
        T = 0f;
        IsPlaying = true;
    }

    public virtual void OnCutsceneTick()
    {
        Elapsed += Time.fixedDeltaTime;
        T = Mathf.Clamp01(Elapsed / Duration);
    }

    public virtual void OnBlendTick(float t) { }
    public virtual void OnBlendOutTick(float t) { }
    public virtual void OnCutsceneSkip() { }
    public virtual void OnCutsceneEnd() => IsPlaying = false;

    public virtual void OnCutsceneUpdate() { }
    public virtual void OnCutsceneLateUpdate() { }
    public void SetBlendDelayActive(bool active) => BlendDelayActive = active;
}