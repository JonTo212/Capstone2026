using UnityEngine;

public class setGroundLayer : MonoBehaviour
{
    private void Start()
    {
        SetLayerRecursively(transform);
    }

    void SetLayerRecursively(Transform parent)
    {
        foreach (Transform child in parent)
        {
            child.gameObject.layer = LayerMask.NameToLayer("Ground");
            SetLayerRecursively(child);
        }
    }
}




