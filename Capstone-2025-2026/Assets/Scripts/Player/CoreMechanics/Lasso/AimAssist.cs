using UnityEngine;

public enum AimAssistType
{
    None,
    Snap,
    Buffer
}

public class AimAssist
{
    private Prop _currentlyHighlightedProp;
    private LayerMask tetherLayerIgnore = ~(1 << LayerMask.NameToLayer("Tether"));

    #region Closest Target (for snap)
    public Prop GetClosestTarget(Camera cam, Vector3 origin, float range)
    {
        Collider[] hits = Physics.OverlapSphere(origin, range);
        float nearestDist = Mathf.Infinity;
        Prop nearestObj = null;

        foreach (Collider col in hits)
        {
            //check if interactable
            Prop prop = col.GetComponentInParent<Prop>();
            if (prop == null) continue;

            //make sure object is in view of camera 
            Vector3 closestPoint = col.ClosestPoint(cam.transform.position);
            Vector3 vp = cam.WorldToViewportPoint(closestPoint);
            if (vp.z < 0 || vp.x < 0 || vp.x > 1 || vp.y < 0 || vp.y > 1) continue;

            //line of sight check
            Vector3 toTarget = cam.transform.position - closestPoint;
            if (Physics.Raycast(cam.transform.position, toTarget.normalized, out RaycastHit hit, toTarget.magnitude))
            {
                if (hit.transform.GetComponentInParent<Prop>() != prop) continue;
            }

            Vector2 screenCenter = new Vector2(0.5f, 0.5f);
            Vector2 screenPos = new Vector2(vp.x, vp.y);
            float screenDist = Vector2.SqrMagnitude(screenCenter - screenPos);

            if (screenDist < nearestDist)
            {
                nearestDist = screenDist;
                nearestObj = prop;
            }
        }

        return nearestObj;
    }
    #endregion

    #region Main Aim Assist Function
    public RaycastHit? GetAssistHitPoint(Camera cam, Vector3 origin, float range, AimAssistType type, float bufferRadius) //the ? means it can return null
    {
        Ray directHitRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        switch (type)
        {
            case AimAssistType.Snap:
                if (GetSnapHit(cam, origin, range, out RaycastHit snapHit)) 
                    return snapHit;
                break;

            case AimAssistType.Buffer:
                if (GetDirectHit(directHitRay, range, out RaycastHit directHit))
                    return directHit;
                if (GetDynamicBufferHit(cam, directHitRay, range, 0f, bufferRadius, out RaycastHit bufferHit))
                    return bufferHit;
                break;

            case AimAssistType.None:
            default:
                if (GetDirectHit(directHitRay, range, out directHit))
                    return directHit;
                break;
        }
        return null;
    }
    #endregion

    #region Snap

    private bool GetSnapHit(Camera cam, Vector3 origin, float range, out RaycastHit hit)
    {
        hit = new RaycastHit();
        Prop target = GetClosestTarget(cam, origin, range); //get closest target to middle of screen, already has prop check
        if (target != null)
        {
            Vector3 targetPos = target.transform.position;
            Ray snapRay = new Ray(cam.transform.position, (targetPos - cam.transform.position).normalized);
            if (Physics.Raycast(snapRay, out RaycastHit snapHit, range))
            {
                hit = snapHit;
                return true;
            }
        }
        return false;
    }

    #endregion

    #region Direct Hit

    private bool GetDirectHit(Ray ray, float range, out RaycastHit hit)
    {
        hit = new RaycastHit();
        if (Physics.Raycast(ray, out RaycastHit bufferHit, range, tetherLayerIgnore))
        {
            if (bufferHit.transform.GetComponentInParent<Prop>() != null)
            {
                hit = bufferHit;
                return true;
            }
        }
        return false;
    }

    #endregion

    #region Buffer Hit

    private bool GetDynamicBufferHit(Camera cam, Ray ray, float range, float minRadius, float maxRadius, out RaycastHit bestHit)
    {
        bestHit = new RaycastHit();
        bool foundProp = false;
        float closestSqrDistance = float.MaxValue;

        int steps = 10; //number of slices along the ray (i.e. number of times it increases in radius)
        Vector3 origin = ray.origin;

        for (int i = 1; i <= steps; i++)
        {
            //% along the 'cone'
            float startPercent = (float)i / steps;
            float endPercent = (float)(i + 1) / steps;

            //start and end points of current segment
            float startDistance = startPercent * range;
            float endDistance = endPercent * range;
            float segmentRadius = Mathf.Lerp(minRadius, maxRadius, endPercent);

            //length + origin of segment for spherecast
            float segmentLength = endDistance - startDistance;
            Vector3 segmentOrigin = ray.origin + ray.direction * startDistance;

            //only spherecast the current segment for 'growing cone' effect
            RaycastHit[] hits = Physics.SphereCastAll(segmentOrigin, segmentRadius, ray.direction, segmentLength, tetherLayerIgnore);
            foreach (RaycastHit hit in hits)
            {
                if (hit.transform.GetComponentInParent<Prop>() == null)
                    continue;

                float sqrDistance = (origin - hit.point).sqrMagnitude;
                if (sqrDistance < closestSqrDistance)
                {
                    closestSqrDistance = sqrDistance;
                    bestHit = hit;
                    foundProp = true;
                }
            }

            if (foundProp)
                break;
        }

        return foundProp;
    }



    #endregion

    #region Highlight
    public void HighlightSelectedProp(Prop selectedProp, bool isDisabled)
    {
        if (isDisabled)
        {
            if (_currentlyHighlightedProp != null)
            {
                _currentlyHighlightedProp.ActivateOutline(false);
                _currentlyHighlightedProp = null;
            }
            return;
        }

        //turn off highlight for old prop
        if (_currentlyHighlightedProp != null && _currentlyHighlightedProp != selectedProp)
        {
            _currentlyHighlightedProp.ActivateOutline(false);
        }

        //turn on highlight for current prop
        if (selectedProp != null)
        {
            selectedProp.ActivateOutline(true);
            _currentlyHighlightedProp = selectedProp;
        }
        else
        {
            _currentlyHighlightedProp = null;
        }
    }
    #endregion
}