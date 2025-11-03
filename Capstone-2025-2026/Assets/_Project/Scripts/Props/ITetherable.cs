using UnityEngine;

public interface ITetherable
{
    void OnAttachTether();
    void OnTetherPull(GameObject tether, Transform targetAnchorTransform, Transform targetObjectTransform, ConfigurableJoint joint);
    void OnDetachTether(GameObject tether, Transform targetAnchorTransform, Transform targetObjectTransform, ConfigurableJoint joint);
}
