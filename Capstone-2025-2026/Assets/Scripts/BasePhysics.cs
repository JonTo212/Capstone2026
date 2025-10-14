using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewObjectType", menuName = "Custom Physics/Object Data")]
public class BasePhysics : ScriptableObject
{
    public float frictionCoefficient;
    public enum MaterialType
    {
        Wood,
        Metal,
        Stone,
        None,
    }

    public Dictionary<BasePhysics.MaterialType, float> materialDensities = new Dictionary<BasePhysics.MaterialType, float>()
    {
        { MaterialType.Wood, 0.7f },
        { MaterialType.Metal, 7.87f },
        { MaterialType.Stone, 2.55f},
        { MaterialType.None, 1f}
    }; 
}