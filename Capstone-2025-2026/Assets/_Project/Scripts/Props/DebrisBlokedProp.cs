using FMODUnity;
using System.IO;
using UnityEngine;

public class DebrisBlokedProp : EnvironmentalProp
{
    [SerializeField] private GameObject[] destructableDebris;
    [SerializeField] private Rigidbody[] kinematicBlokingDebris;
    [SerializeField] private Vector3 directionToBreakFree = Vector3.up;
    [SerializeField] private float directionTolerance = 0.2f;
    [SerializeField] private float minimumForceFromTether = 100f;

    [SerializeField] private bool isFree = false;

    [SerializeField] ParticleSystem dustBurstParticle;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Rb.isKinematic = true;
    }

    // Update is called once per frame
    protected override void Update()
    {
        if(isFree == false)
        {
            if(IsForceDirectionAligned() && IsForceEnough())
            {
                ActivateAllDebrisRigidbody();
                DestroyAllDestructableDebris();
                Rb.isKinematic = false;
                isFree = true;

                //particle
                dustBurstParticle.Play();
            }
        }

    }

    private bool IsForceDirectionAligned()
    {
        return Vector3.Dot(directionToBreakFree.normalized, GetForcesFromJoint().normalized) > (1f - directionTolerance);
    }

    private bool IsForceEnough()
    {
        return GetForcesFromJoint().magnitude > minimumForceFromTether;
    }

    private void ActivateAllDebrisRigidbody()
    {
        foreach (Rigidbody debrisRb in kinematicBlokingDebris)
        {

            RuntimeManager.PlayOneShot("event:/TetherRockBreak", transform.position);
            if (debrisRb != null) debrisRb.isKinematic = false;
        }

    }

    private void DestroyAllDestructableDebris()
    {
        foreach(GameObject destructables in destructableDebris)
        {
            if (destructables != null) Destroy(destructables);
        }
    }
}
