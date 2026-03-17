using UnityEngine;
using System.Collections;
using FMODUnity;
using UnityEngine.UI;

public class PickupNPCProp : Prop
{
    private Vector3 defaultLocalScale;
    private Coroutine animCoroutine;
    [SerializeField] private float deflatedScale = 0.2f;
    [SerializeField] private CritterInstance critterInstanceScript;

    [SerializeField] private GameObject grabIndicator;

    //boolean if you want an NPC to summon or destroy objects after its saved
    [SerializeField] private bool keyNPC = false;
    [SerializeField] Transform[] objectsToEnable; 
    [SerializeField] Transform[] objectsToDestroy;

    //particles
    [SerializeField] private ParticleSystem dustParticle;

    public EndSequeenceTracker endTrack;





    private void Awake()
    {
        critterInstanceScript = GetComponent<CritterInstance>();

        base.Init();
        defaultLocalScale = transform.localScale;

        //disable objects if its a key npc
        if (keyNPC)
        {
            foreach (Transform t in objectsToEnable)
            {
                t.gameObject.SetActive(false);
            }
        }
    }

    public void OnCaptureStart(float captureDuration)
    {
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(Deflate(captureDuration, deflatedScale));

        DestroyAllAttachedTethers();

        RuntimeManager.PlayOneShot("event:/NPCSave", transform.position);

        //tell UI that you got a puff
        critterInstanceScript.BeRescued();

        if(endTrack) endTrack.EndSequence();

        if (keyNPC) KeyNPCAction();// make npc summon object or destory object
    }

    private void KeyNPCAction()
    {
        //Voice Line
        GetComponent<DialogueTrigger>().CreateNPCDialogue();


        //enable objects
        foreach (Transform t in objectsToEnable)
        {
            t.gameObject.SetActive(true);

            //FX
            RuntimeManager.PlayOneShot("event:/Fanfare", t.position);
            // Spawn particle
            Instantiate(dustParticle, t.position, Quaternion.identity);

        }

        //destroy objects
        foreach (Transform t in objectsToDestroy)
        {
            Destroy(t.gameObject);

            //FX
            RuntimeManager.PlayOneShot("event:/WallBreak", transform.position);
            // Spawn particle
            Instantiate(dustParticle, t.position, Quaternion.identity);
        }
    }

    public override void ActivateOutline(bool activate)
    {
        base.ActivateOutline(activate);
        grabIndicator.SetActive(activate);
    }

    public void OnCaptureInterrupted(float captureDuration)
    {
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(Inflate(captureDuration, true));
    }

    public IEnumerator Inflate(float duration, bool enableCollider)
    {

        float elapsedTime = 0;
        Vector3 startScale = transform.localScale;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            transform.localScale = Vector3.Lerp(startScale, defaultLocalScale, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localScale = defaultLocalScale;
        if (enableCollider) GetComponent<Collider>().enabled = true;
        animCoroutine = null;
    }

    public IEnumerator Deflate(float duration, float scale)
    {
        float elapsedTime = 0;
        Vector3 startScale = transform.localScale;
        GetComponent<Collider>().enabled = false;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            transform.localScale = Vector3.Lerp(startScale, defaultLocalScale * scale, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localScale = defaultLocalScale * scale;
        animCoroutine = null;
    }
}
