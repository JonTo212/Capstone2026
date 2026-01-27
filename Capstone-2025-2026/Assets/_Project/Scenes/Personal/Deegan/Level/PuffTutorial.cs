using UnityEngine;

public class PuffTutorial : PluckOutProp
{
    [SerializeField] Transform helpText;
    [SerializeField] Transform savedText;
    [SerializeField] Transform savedFishTransform;

    private void Awake()
    {
        Init();
    }

    protected override void OnPluck()
    {
        base.OnPluck();
    }
}
