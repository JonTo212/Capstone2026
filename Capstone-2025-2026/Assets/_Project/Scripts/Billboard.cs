using UnityEngine;

public class Billboard : MonoBehaviour
{
    // https://discussions.unity.com/t/how-i-can-create-an-sprite-that-always-look-at-the-camera/16891



    void Update()
    {
        transform.LookAt(Camera.main.transform.position, Vector3.up);
    }
}
