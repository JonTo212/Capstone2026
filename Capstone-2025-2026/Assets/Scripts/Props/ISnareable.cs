using UnityEngine;

public interface ISnareable
{
    Rigidbody Rb { get; }
    void OnSnare();
    void OnRelease();
}
