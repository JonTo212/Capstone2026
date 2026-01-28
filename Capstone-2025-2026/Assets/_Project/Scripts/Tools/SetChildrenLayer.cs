using UnityEngine;

public class SetChildrenLayer : MonoBehaviour
{
    [SerializeField] private LayerMask desiredLayer;
    private int layerIndex;

    private void Start()
    {
        layerIndex = ToLayer(desiredLayer.value);
        SetLayerRecursively(transform);
    }

    private void SetLayerRecursively(Transform parent)
    {
        foreach (Transform child in parent)
        {
            child.gameObject.layer = layerIndex;
            SetLayerRecursively(child);
        }
    }

    private int ToLayer(int bitmask)
    {
        int result = bitmask > 0 ? 0 : 0;
        while (bitmask > 1)
        {
            bitmask >>= 1; //shifts bits by one to the right
            result++;
        }
        return result;
    }
}




