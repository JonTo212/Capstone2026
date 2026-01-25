using System.Collections.Generic;
using UnityEngine;

public class RailSystemHandler : MonoBehaviour
{
    private List<Rail> rails = new List<Rail>();
    private List<Rail> railsInside = new List<Rail>();
    private Rail currentActiveRail;
    private Rail nextRail;
    [field: SerializeField] public Prop DesignatedProp { get; private set; }

    private void Awake()
    {
        foreach (Transform child in transform)
        {
            if (child.TryGetComponent(out Rail rail))
            {
                rails.Add(rail);
            }
        }
    }


    private void FixedUpdate()
    {
        if (DesignatedProp == null) return;

        if (railsInside.Count == 0)
        {
            currentActiveRail = null;
            return;
        }

        EvaluateBestRail();   // chooses active rail
        ApplyActiveRailConstraints(); // freezes axes based on chosen rail
    }


    private void EvaluateBestRail()
    {
        Vector3 force = DesignatedProp.totalForceApplied;
        float forceMag = force.sqrMagnitude;

        Rail bestRail = null;
        float bestScore = float.MinValue;

        foreach (var rail in railsInside)
        {
            float distance = rail.GetDistanceFromCenterLine(DesignatedProp.transform.position);

            // Distance score (closer = better)
            float distanceScore = 1f / (1f + distance);

            // Force alignment score
            float alignmentScore = 0f;
            if (forceMag > 0.0001f)
            {
                Vector3 dir = rail.GetWorldDirection();
                alignmentScore = Mathf.Abs(Vector3.Dot(force.normalized, dir));
            }

            // Combined score
            float score = distanceScore + alignmentScore * 0.75f;

            if (score > bestScore)
            {
                bestScore = score;
                bestRail = rail;
            }
        }

        if (bestRail != currentActiveRail)
        {
            SwitchToRail(bestRail);
        }
    }

    private void SwitchToRail(Rail newRail)
    {
        currentActiveRail = newRail;

        Vector3 snapPos = newRail.GetLocalSnapPosition(DesignatedProp.transform.position);

        DesignatedProp.transform.position = snapPos;
        DesignatedProp.Rb.position = snapPos;

        DesignatedProp.Rb.linearVelocity = Vector3.zero;
        DesignatedProp.Rb.angularVelocity = Vector3.zero;
    }


    private void ApplyActiveRailConstraints()
    {
        if (currentActiveRail == null)
            return;

        RailAxis axis = currentActiveRail.GetRailDir();

        RigidbodyConstraints c = RigidbodyConstraints.FreezeRotation;

        if (axis != RailAxis.X) c |= RigidbodyConstraints.FreezePositionX;
        if (axis != RailAxis.Y) c |= RigidbodyConstraints.FreezePositionY;
        if (axis != RailAxis.Z) c |= RigidbodyConstraints.FreezePositionZ;

        DesignatedProp.Rb.constraints = c;

        //currentActiveRail.EnforceRailBounds(DesignatedProp);
    }


    public void OnRailEnter(Rail rail, Prop prop)
    {
        if (prop != DesignatedProp) return;

        if(!railsInside.Contains(rail))
            railsInside.Add(rail);


        //RecalculateConstraints(prop);
    }

    public void OnRailExit(Rail rail, Prop prop)
    {
        if (prop != DesignatedProp) return;

        if (railsInside.Contains(rail))
            railsInside.Remove(rail);


        //RecalculateConstraints(prop);
    }

    private void RecalculateConstraints(Prop prop)
    {
        bool canMoveX = false;
        bool canMoveY = false;
        bool canMoveZ = false;

        foreach (Rail r in railsInside)
        {
            RailAxis axis = r.GetRailDir();

            if (axis == RailAxis.X) canMoveX = true;
            if (axis == RailAxis.Y) canMoveY = true;
            if (axis == RailAxis.Z) canMoveZ = true;
        }

        RigidbodyConstraints finalConstraints = RigidbodyConstraints.FreezeRotation;
        if (!canMoveX) finalConstraints |= RigidbodyConstraints.FreezePositionX;
        if (!canMoveY) finalConstraints |= RigidbodyConstraints.FreezePositionY;
        if (!canMoveZ) finalConstraints |= RigidbodyConstraints.FreezePositionZ;

        prop.Rb.constraints = finalConstraints;
    }

    private void SwitchActiveRail(Rail newRail)
    {
        currentActiveRail = newRail;
    }

    private void SnapObjectToRail(Rail rail, Prop prop)
    {
        Vector3 snapPos = rail.GetLocalSnapPosition(prop.transform.position);

        prop.transform.position = snapPos;
        prop.Rb.position = snapPos;

        prop.Rb.linearVelocity = Vector3.zero;
        prop.Rb.angularVelocity = Vector3.zero;

        prop.Rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    private void CheckCloserRail()
    {
        if(railsInside.Count < 2) return;

        Vector3 force = DesignatedProp.totalForceApplied;
        if (force.sqrMagnitude < 0.001f) return;

        Vector3 currentDir = currentActiveRail.GetWorldDirection();
        Vector3 nextDir = nextRail.GetWorldDirection();

        float towardCurrent = Mathf.Abs(Vector3.Dot(force.normalized, currentDir));
        float towardNext = Mathf.Abs(Vector3.Dot(force.normalized, nextDir));
    }
}
