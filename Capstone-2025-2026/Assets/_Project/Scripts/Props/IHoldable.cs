using UnityEngine;

public interface IHoldable
{
    void OnHold(Transform newParent);
    void OnRelease();
    void OnThrow(Vector3 dir, float magnitude);
}
