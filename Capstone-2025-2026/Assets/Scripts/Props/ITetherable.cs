using UnityEngine;

public interface ITetherable
{
    void AttachTether(Transform tetherPoint);
    void DetachTether();
}
