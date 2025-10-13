using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class InCameraDetector : MonoBehaviour
{
    //https://www.youtube.com/watch?v=XrqesjfcitU

    Camera camera;
    MeshRenderer renderer;
    Plane[] cameraFrustum;
    Collider collider;

    public bool isInCameraView;

    private void Start()
    {
        camera = Camera.main;
        renderer = GetComponent<MeshRenderer>();

        collider = GetComponent<Collider>();

    }

    private void Update()
    {
        cameraFrustum = GeometryUtility.CalculateFrustumPlanes(camera);

        if (GeometryUtility.TestPlanesAABB(cameraFrustum, collider.bounds))
        {
            isInCameraView = true;
        }
        else
        {
            isInCameraView = false;
        }
    }
}


