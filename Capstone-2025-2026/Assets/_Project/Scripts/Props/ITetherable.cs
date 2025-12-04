using UnityEngine;

public interface ITetherable
{
    bool IsTetherPulled { get; }   
    void OnTetherPull(JointTether tether, Transform targetAnchorTransform, Transform targetObjectTransform);
    void OnDetachTether(JointTether tether, Transform targetAnchorTransform, Transform targetObjectTransform);
}
