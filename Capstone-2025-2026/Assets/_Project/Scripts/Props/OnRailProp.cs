using UnityEngine;

public class OnRailProp : Prop
{
    [field: SerializeField] public Transform RailAnchor { get; private set; }
    private void Awake()
    {
        base.Init();
    }
}
