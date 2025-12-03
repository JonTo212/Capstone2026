using UnityEngine;

public interface ITetherable
{
    bool IsTetherPulled { get; }   
    void OnAttachTether();
    void OnTetherPull(JointTether tether, Transform targetAnchorTransform, Transform targetObjectTransform);
    void OnDetachTether(JointTether tether, Transform targetAnchorTransform, Transform targetObjectTransform);
}
