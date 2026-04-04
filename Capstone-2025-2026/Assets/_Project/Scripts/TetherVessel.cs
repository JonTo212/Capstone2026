using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using Unity.Hierarchy;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

public class TetherVessel : EnvironmentalProp
{
    private int numberOfAttachedClaws = 0;

    public float raiseHeight = 2f;
    public float raiseSpeed = 1f;   

    private Vector3 raisePosition;
    private Vector3 nudgePosition; // how far to move vessel when player attached a single tether

    //public ParticleSystem dustParticle;
    //public ParticleSystem windPipeEntranceParticles;

    //private List<ClawSetpiece> attachedClawSetPieces = new List<ClawSetpiece>();

    //states
    public enum VesselState{Stuck, Underground, Surfaced}
    public VesselState currentState = VesselState.Surfaced;

    private Coroutine clawBreakCoroutine;


    [SerializeField] private GameObject destructableDebris;
    [SerializeField] ParticleSystem dustBurstParticle;
    [SerializeField] private bool isFree = false;

    private void Awake()
    {
        Init();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        raisePosition = new Vector3(transform.position.x, transform.position.y + raiseHeight, transform.position.z);
        nudgePosition = new Vector3(transform.position.x, transform.position.y + raiseHeight + 0.5f, transform.position.z);

        currentState = VesselState.Surfaced;
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();

        switch (currentState)
        {
            case VesselState.Stuck:
                StuckState();
                break;
            case VesselState.Underground:
                //UndergroundState();
                break;
            case VesselState.Surfaced:
                SurfacedState();
                break;
        }
    }

    void StuckState()
    {
        if (numberOfAttachedClaws == 3 && clawBreakCoroutine == null)
        {
            //clawBreakCoroutine = StartCoroutine(BreakClawsAfterDelay());
        }
    }

    /*
    void UndergroundState()
    {
        transform.position = Vector3.Lerp(transform.position, raisePosition, raiseSpeed * Time.deltaTime);

        if (dustParticle != null && !dustParticle.isPlaying)
        {
            dustParticle.Play();
        }

        if (Vector3.Distance(transform.position, raisePosition) < 0.1f)
        {
            currentState = VesselState.Surfaced;

        }
    }
    */

    void SurfacedState()
    {
        // Play particle here as the transition occurs

        if (attachedTethers.Count >= 1) //&& Vector3.Dot(GetForcesFromJoint().normalized, Vector3.up) > 0.7)
        {
            Rb.isKinematic = false;

            if (isFree == false)
            {
                DestroyAllDestructableDebris();
                Rb.isKinematic = false;
                isFree = true;
            }
        }
    }


    
    public void IncreaseAttachedClawCount(ClawSetpiece claw)
    {
        numberOfAttachedClaws++;
        //attachedClawSetPieces.Add(claw);
    }

    public void DecreaseAttachedClawCount(ClawSetpiece claw)
    {
        numberOfAttachedClaws++;
       // attachedClawSetPieces.Remove(claw);
    }

    /*
    IEnumerator BreakClawsAfterDelay()
    {
        yield return new WaitForSeconds(2f);

        //AudioManager.Instance.PlaySFX(AudioManager.Instance.GrabberExplode, 1, .8f);
        RuntimeManager.PlayOneShot("event:/ClawBreak", transform.position);

        Destroy(attachedClawSetPieces[1].gameObject);
        Destroy(attachedClawSetPieces[2].gameObject);

        currentState = VesselState.Underground;
        windPipeEntranceParticles.Play();
    }
    */


    private void DestroyAllDestructableDebris()
    {
        RuntimeManager.PlayOneShot("event:/TetherRockBreak", transform.position);

        //particle
        dustBurstParticle.Play();

        if (destructableDebris != null) Destroy(destructableDebris);



    }
}
