using UnityEngine;

public interface ISnareable
{
    bool IsSnared { get; }
    Rigidbody Rb { get; }
    void OnSnare(PlayerRefData playerData);
    void OnRelease();
}
