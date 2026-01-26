using System.Collections.Generic;
using UnityEngine;

public class RailSystemHandler : MonoBehaviour
{
    private List<Rail> rails = new List<Rail>();
    private List<Rail> railsInside = new List<Rail>();
    private Rail currentActiveRail;
    private Rail nextRail;
    [field: SerializeField] public OnRailProp DesignatedProp { get; private set; }

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
        ApplyRailConstraints(); // freezes axes based on chosen rail
    }


    private void EvaluateBestRail()
    {
        Vector3 force = DesignatedProp.Rb.linearVelocity;
        float forceMag = force.sqrMagnitude;

        Rail bestRail = null;
        float bestScore = float.MinValue;

        foreach (var rail in railsInside)
        {
            float distance = rail.GetDistanceFromCenterLine(DesignatedProp.RailAnchor.position);
            float distanceScore = 1f / (1f + distance);

            float alignmentScore = 0f;
            if (forceMag > 0.0001f)
            {
                Vector3 dir = rail.GetWorldDirection();
                alignmentScore = Mathf.Abs(Vector3.Dot(force.normalized, dir));
            }

            float score = distanceScore + alignmentScore;

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

        Vector3 anchorWorld = DesignatedProp.RailAnchor.position; 
        Vector3 snappedAnchor = newRail.GetLocalSnapPosition(anchorWorld);

        Vector3 delta = snappedAnchor - anchorWorld; 
        DesignatedProp.transform.position += delta; 
        DesignatedProp.Rb.position += delta;
    }

    [SerializeField] private float centerlineActivationRadius = 0.25f;

    private List<Rail> GetTrulyInsideRails()
    {
        List<Rail> valid = new List<Rail>();

        foreach (var rail in railsInside)
        {
            float dist = rail.GetDistanceFromCenterLine(DesignatedProp.RailAnchor.position);

            if (dist <= centerlineActivationRadius)
                valid.Add(rail);
        }

        return valid;
    }

    private void ApplyRailConstraints()
    {
        bool allowX = false;
        bool allowY = false;
        bool allowZ = false;

        List<Rail> trulyInsideRails = GetTrulyInsideRails();
        if(trulyInsideRails.Count == 0)
        {
            trulyInsideRails = railsInside;
        }

        foreach (var rail in GetTrulyInsideRails())
        {
            switch (rail.GetRailDir())
            {
                case RailAxis.X: allowX = true; break;
                case RailAxis.Y: allowY = true; break;
                case RailAxis.Z: allowZ = true; break;
            }
        }

        RigidbodyConstraints c = RigidbodyConstraints.FreezeRotation;

        if (!allowX) c |= RigidbodyConstraints.FreezePositionX;
        if (!allowY) c |= RigidbodyConstraints.FreezePositionY;
        if (!allowZ) c |= RigidbodyConstraints.FreezePositionZ;

        DesignatedProp.Rb.constraints = c;
        currentActiveRail.EnforceRailBounds(DesignatedProp);
    }



    public void OnRailEnter(Rail rail, Prop prop)
    {
        if (prop != DesignatedProp) return;

        if(!railsInside.Contains(rail))
            railsInside.Add(rail);
    }

    public void OnRailExit(Rail rail, Prop prop)
    {
        if (prop != DesignatedProp) return;

        if (railsInside.Contains(rail))
            railsInside.Remove(rail);
    }
}
