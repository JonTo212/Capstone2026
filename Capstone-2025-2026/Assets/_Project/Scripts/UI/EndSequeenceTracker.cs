using FMODUnity;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;

public class EndSequeenceTracker : MonoBehaviour
{

    public GameObject[] Anchor;
    public GameObject[] EnabledArray;
    public GameObject[] DisabledArray;
    public Animator SpaceShip;

    [SerializeField] private int ropeSegmentCount = 50; // reduced for performance
    [SerializeField] private float damper = 15f;
    [SerializeField] private float strength = 800f;
    [SerializeField] private float velocity = 15f;
    [SerializeField] private float waveCount = 3f;
    [SerializeField] private float waveHeight = 2f;
    [SerializeField] private AnimationCurve affectCurve;

    private Spring spring;
    private Vector3 currentPullPos;
    public LineRenderer forceConnections;

    public void Start()
    {
        DrawAnchors();
    }

    #region AnchorVisuals

    public void DrawAnchors()
    {
        for(int i = 0; i<1; i++)
        {
            Vector3 targetPoint = Anchor[i].transform.position;
            forceConnections.SetPosition(0, transform.position);
            forceConnections.SetPosition(1, targetPoint);
        }
    }
    /*
    private void DetachRope(int i)
    {
        if (forceConnections[i].positionCount == 0)
        {
            spring.SetVelocity(velocity);
            forceConnections[i].positionCount = ropeSegmentCount + 1;
        }

        spring.SetDamper(damper);
        spring.SetStrength(strength);
        spring.Update(Time.deltaTime);

        Vector3 startPoint = lassoScript.HoldPos.position;
        Vector3 targetPoint = lassoScript.ProjectilePosition;
        Vector3 up = Quaternion.LookRotation((targetPoint - startPoint).normalized) * Vector3.up;

        if (projectileActive)
        {
            currentPullPos = Vector3.Lerp(currentPullPos, targetPoint, Time.deltaTime * velocity);
        }
        else
        {
            currentPullPos = lassoScript.SnaredObject.transform.position;
        }

        for (int j = 0; j < ropeSegmentCount + 1; j++)
        {
            float delta = j / (float)ropeSegmentCount;
            Vector3 offset = up * waveHeight * Mathf.Sin(delta * waveCount * Mathf.PI) * spring.Value * affectCurve.Evaluate(delta);
            Vector3 ropePos = Vector3.Lerp(startPoint, currentPullPos, delta) + offset;

            forceConnections[i].SetPosition(j, ropePos);
        }
    }*/

    #endregion 

    #region Functional Code
    public void UpdateSupports(int targettedSupport)
    {
        Destroy(Anchor[targettedSupport]);
        forceConnections.gameObject.SetActive(false);
        Anchor[targettedSupport] = null;

        if (Anchor[0] == null)
        {
            EndSequence();
        }
    } 

    public void EndSequence()
    {
        //Play Scene
        EnabledThings();
        SpaceShip.enabled = true;
        DisabledThings();

        Debug.Log("THE END SCENE HAPPENED");
    }

    public void EnabledThings()
    {
        foreach(GameObject _object in EnabledArray){
            _object.SetActive(true);
        }
    }

    public void DisabledThings()
    {
        foreach(GameObject _object in DisabledArray){
            _object.SetActive(false);
        }
    }

    #endregion
}
