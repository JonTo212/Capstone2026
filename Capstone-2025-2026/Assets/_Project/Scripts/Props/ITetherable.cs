using UnityEngine;

public interface ITetherable
{
    void OnAttachTether();
    void OnTetherPull(GameObject tether, Transform target, ConfigurableJoint joint);
    void OnDetachTether(GameObject tether, Transform target, ConfigurableJoint joint);
}
