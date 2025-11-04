using System.Collections.Generic;
using UnityEngine;

public interface IGrabPointGenerator
{
    public List<Transform> GeneratePoints(Collider col, int rows, int columns);
}
