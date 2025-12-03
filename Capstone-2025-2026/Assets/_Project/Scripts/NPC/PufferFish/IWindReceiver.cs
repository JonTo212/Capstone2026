using UnityEngine;

public interface IEnvironmentalElement
{
    Vector3 CalculateForce(Rigidbody rb);
}
