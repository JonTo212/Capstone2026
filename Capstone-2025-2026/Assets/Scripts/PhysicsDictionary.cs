using System.Collections.Generic;
using UnityEngine;

public static class PhysicsDictionary
{
    public enum MaterialType
    {
        None,
        Wood,
        Metal,
        Stone,
    }

    public static Dictionary<MaterialType, PhysicsMaterial> frictionMaterial = new Dictionary<MaterialType, PhysicsMaterial>()
    {
        { MaterialType.None, Resources.Load<PhysicsMaterial>("Assets/PhysicsMaterials/None")},
        { MaterialType.Wood, Resources.Load<PhysicsMaterial>("Assets/PhysicsMaterials/Wood")},
        { MaterialType.Metal, Resources.Load<PhysicsMaterial>("Assets/PhysicsMaterials/Metal")},
        { MaterialType.Stone, Resources.Load<PhysicsMaterial>("Assets/PhysicsMaterials/Stone")}
    };

}
