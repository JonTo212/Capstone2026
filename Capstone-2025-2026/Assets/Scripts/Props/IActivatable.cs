using UnityEngine;

public interface IActivatable
{
    bool IsActive { get; }
    void Activate();
    void Deactivate();
}
