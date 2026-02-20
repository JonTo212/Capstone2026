using UnityEngine;

public interface ISnareable
{
    bool IsSnared { get; }
    Rigidbody Rb { get; }
    void OnSnare(Lasso lassoRef);
    void OnRelease();
}
