using UnityEngine;

public class Nail : MonoBehaviour
{
    public int nailNumber;
    public EndSequeenceTracker tracker;

    void OnDestroy()
    {
        tracker.UpdateSupports(nailNumber);
    }
}
