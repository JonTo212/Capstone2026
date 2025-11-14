using UnityEngine;

public interface ITetherable
{
    void OnAttachTether();
    void OnTetherPull(JointTether tether, Transform targetAnchorTransform, Transform targetObjectTransform);
    void OnDetachTether(JointTether tether, Transform targetAnchorTransform, Transform targetObjectTransform);
}
