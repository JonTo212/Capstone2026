using System.Runtime.CompilerServices;
using UnityEngine;

public class SlowMotionTetherPlacing : MonoBehaviour
{
    Camera _playerCamera;
    [SerializeField] private float slowMotionAmount = 0.1f;
    private Prop currentHeldProp;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _playerCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //Get all placement points
    //Get neares placement point
    //Click to connect tether to target with nearest tether point

    public void EnterSlowTetherMode(Prop currentHeldProp)
    {
        Time.timeScale = slowMotionAmount;
        this.currentHeldProp = currentHeldProp;
    }

    public void HandleTetherMode()
    {
        Time.fixedDeltaTime = Time.timeScale * 0.02f;
    }

    public void ExitTetherMode()
    {
        Time.timeScale = 1f;
    }

    private Transform GetClosestAttachmentPoint(Prop prop, Vector3 target)
    {
        if(prop.GrabPoints.Count  < 1)
        {
            return prop.transform;
        }

        Transform[] points = prop.GrabPoints.ToArray();

        Transform closestPoint = points[0];

        for (int i = 0; i < points.Length; i++)
        {
            if (Vector3.Distance(points[i].position, target) < Vector3.Distance(closestPoint.position, target))
            {
                closestPoint = points[i];  
            }
        }

        return closestPoint;
    }

    private void TetherPlacementPreview()
    {

    }
}
