using UnityEngine;

public abstract class CutsceneBase : MonoBehaviour
{
    [field: SerializeField] public float Duration { get; private set; }
    [field: SerializeField] public float BlendInDelay { get; private set; }
    [field: SerializeField] public float BlendInTime { get; private set; }
    [field: SerializeField] public float BlendOutTime { get; private set; }

    public float T { get; private set; }
    public bool IsPlaying { get; private set; }
    public bool BlendDelayActive { get; private set; }
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

    public virtual void OnCutsceneEnd()
    {
        IsPlaying = false;
    }
    public virtual void OnCutsceneUpdate() { }
    public virtual void OnCutsceneLateUpdate() { }
    public void SetBlendDelayActive(bool active)
    {
        BlendDelayActive = active;
    }
}