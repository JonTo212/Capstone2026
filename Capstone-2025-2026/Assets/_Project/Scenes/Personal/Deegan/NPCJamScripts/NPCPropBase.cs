using System.Collections.Generic;
using UnityEngine;

public class NPCPropBase : Prop
{
    Lasso lasso;

    private void Awake()
    {
        Init();
    }

    private void Start()
    {
        lasso = GameObject.FindFirstObjectByType<Lasso>();
    }

    protected override void Update()
    {
        base.Update();

        if(IsSnared)
        {

        }
    }

    public void DetachAllTether()
    {
        List<JointTether> tethers = new List<JointTether>(attachedTethers);
        foreach(JointTether tether in tethers)
        {
            tether.DestroyTether();
        }
    }

    public void DetachLasso()
    {
        lasso.HandleObjectReleased();
    }
}
