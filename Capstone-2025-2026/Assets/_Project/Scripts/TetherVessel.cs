using UnityEngine;

public class TetherVessel : EnvironmentalProp
{
    public bool exposeDoor = false;

    public float raiseHeight = 2f;
    public float raiseSpeed = 1f;   

    private Vector3 raisePosition;
    private Vector3 nudgePosition; // how far to move vessel when player attached a single tether

    private EnvironmentalProp environmentalPropScript;

    public ParticleSystem dustParticle;

    //states
    public enum VesselState{Underground, Surfaced}
    public VesselState currentState;


    private void Awake()
    {
        Init();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        raisePosition = new Vector3(transform.position.x, transform.position.y + raiseHeight, transform.position.z);
        nudgePosition = new Vector3(transform.position.x, transform.position.y + raiseHeight + 0.5f, transform.position.z);

        environmentalPropScript = GetComponent<EnvironmentalProp>();
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();

        switch (currentState)
        {
            case VesselState.Underground:
                UndergroundState();
                break;
            case VesselState.Surfaced:
                SurfacedState();
                break;
        }
    }

    void UndergroundState()
    {
        if (exposeDoor)
        {
            transform.position = Vector3.Lerp(transform.position, raisePosition, raiseSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, raisePosition) < 0.1f)
            {
                currentState = VesselState.Surfaced;
            }
        }
    }

    void SurfacedState()
    {
        

        // Play particle here as the transition occurs
        if (dustParticle != null && !dustParticle.isPlaying)
        {
            dustParticle.Play();
        }


        if (attachedTethers.Count == 1)
        {
            transform.position = Vector3.Lerp(transform.position, nudgePosition, raiseSpeed * Time.deltaTime);
        }

        if (attachedTethers.Count == 2)
        {
            Rb.isKinematic = false;
        }
    }
}
