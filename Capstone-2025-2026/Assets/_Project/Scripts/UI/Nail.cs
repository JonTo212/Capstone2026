using UnityEngine;

public class Nail : MonoBehaviour
{
    public int nailNumber;
    public EndSequeenceTracker tracker;

    void OnDestroy()
    {
        //GetComponent<LineRenderer>().enabled = false;
        tracker.UpdateSupports(nailNumber);
    }
}
