using System;
using UnityEngine;

public enum WeightComparisonResult
{
    Object1,
    Object2,
    Equal
}

public static class WeightComparison
{
    public static WeightComparisonResult CompareObjectWeights(GameObject object1, GameObject object2)
    {
        //currently use rb mass as weight, but we could move it to the tetherable component or something
        if (object1.TryGetComponent(out Rigidbody rb1) && object2.TryGetComponent(out Rigidbody rb2))
        {
            float obj1Weight = rb1.mass;
            float obj2Weight = rb2.mass;
            return DetermineHeavierObject(obj1Weight, obj2Weight);
        }
        else
        {
            return WeightComparisonResult.Equal;
        }
    }

    private static WeightComparisonResult DetermineHeavierObject(float weight1, float weight2)
    {
        if (weight1 > weight2)
            return WeightComparisonResult.Object1;
        else if (weight2 > weight1)
            return WeightComparisonResult.Object2;
        else
            return WeightComparisonResult.Equal;
    }

    public static void ApplyWeightedForce(Rigidbody rb1, Vector3 rb1TargetPos, Rigidbody rb2, Vector3 rb2TargetPos, float forceMagnitude, ForceMode mode)
    {
        if (rb1 == null || rb2 == null) return;

        WeightComparisonResult comparison = DetermineHeavierObject(rb1.mass, rb2.mass);
        Vector3 rb1Dir = (rb1TargetPos - rb1.position).normalized; //where rb1 should move to (e.g. player moves towards point)
        Vector3 rb2Dir = (rb2TargetPos - rb2.position).normalized; //where rb2 should move to (e.g. object moves towards holdPos)

        switch (comparison)
        {
            //if object 1 is heavier, pull object 2 towards its target point
            case WeightComparisonResult.Object1:
                rb2.AddForce(rb2Dir * forceMagnitude, mode);
                break;

            //if object 2 is heavier, pull object 1 towards its target point
            case WeightComparisonResult.Object2:
                rb1.AddForce(rb1Dir * forceMagnitude, mode);
                break;

            //if equal, pull both objects towards their target points with half the force
            case WeightComparisonResult.Equal:
                rb1.AddForce(rb1Dir * (forceMagnitude * 0.5f), mode);
                rb2.AddForce(rb2Dir * (forceMagnitude * 0.5f), mode);
                break;
        }
    }
}
