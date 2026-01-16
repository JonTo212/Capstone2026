using UnityEngine;

public class DynamicPropAfterSnare : Prop
{
    private void Awake()
    {
        Init();
    }

    protected override void Update()
    {
        base.Update();

        if(IsSnared)
        {
            Rb.isKinematic = false;
        }
    }
}
