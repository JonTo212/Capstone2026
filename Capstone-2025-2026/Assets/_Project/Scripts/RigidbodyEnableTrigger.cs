using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class PropRBEnableTrigger : MonoBehaviour
{
    private BoxCollider box;
    private List<Rigidbody> results = new List<Rigidbody>();
    private Dictionary<Rigidbody, bool> rigidbodyStartingStatus = new Dictionary<Rigidbody, bool>();

    void Awake() => box = GetComponent<BoxCollider>();
    void Start() => ActivateObjectsInsideBox();

    void ActivateObjectsInsideBox()
    {
        Collider[] childColliders = new Collider[512];
        int count = Physics.OverlapBoxNonAlloc(
            box.bounds.center, box.bounds.extents,
            childColliders, box.transform.rotation, ~0);

        for (int i = 0; i < count; i++)
        {
            Rigidbody rb = childColliders[i].attachedRigidbody;
            if (rb != null && !results.Contains(rb))
            {
                results.Add(rb);
                rigidbodyStartingStatus[rb] = rb.isKinematic;
                rb.isKinematic = true;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        print("waking up");
        foreach (Rigidbody rb in results)
        {
            rb.isKinematic = rigidbodyStartingStatus[rb];
            rb.WakeUp();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        print("sleeping");
        foreach (Rigidbody rb in results)
        {
            rb.isKinematic = true; // re-freeze on exit
            rb.Sleep();
        }
    }
}