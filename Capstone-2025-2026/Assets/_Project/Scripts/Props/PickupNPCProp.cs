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
    [SerializeField] private ParticleSystem captureStars;
    [SerializeField] private ParticleSystem dustParticle;

    private NPCKeyCutscene spawnCutscene;

    [SerializeField] private Transform cutsceneStartPos;
    [SerializeField] private Transform lookAtTarget;
    [SerializeField] private float cutsceneDuration;
    [SerializeField] private float cutsceneHoldFraction;
    [SerializeField] private float cutsceneBlendInDelay;
    [SerializeField] private float cutsceneBlendInTime;
    [SerializeField] private EndSequeenceTracker endTrack;


    private void Awake()
    {
        base.Init();

        critterInstanceScript = GetComponent<CritterInstance>();
        spawnCutscene = Camera.main.GetComponent<NPCKeyCutscene>();
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

        Instantiate(captureStars, this.transform.position, Quaternion.identity);

        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(Deflate(captureDuration, deflatedScale));

        DestroyAllAttachedTethers();



        RuntimeManager.PlayOneShot("event:/NPCSave", transform.position);
        if(endTrack) endTrack.EndSequence();

        //tell UI that you got a puff
        critterInstanceScript.BeRescued();

        if (keyNPC)
        {
            Invoke("KeyNPCAction", 2.0f);
        }
           // KeyNPCAction(); // make npc summon object or destroy object
    }

    private void KeyNPCAction()
    {


        //Voice Line
        GetComponent<DialogueTrigger>().CreateNPCDialogue();


        //enable objects
        if (objectsToEnable.Length > 0)
        {
            foreach (Transform t in objectsToEnable)
            {
                if (t == null) continue;
                t.gameObject.SetActive(true);

                //FX
                RuntimeManager.PlayOneShot("event:/Fanfare", t.position);
                // Spawn particle
                Instantiate(dustParticle, t.position, Quaternion.identity);
            }
        }

        //destroy objects
        if (objectsToDestroy.Length > 0)
        {
            foreach (Transform t in objectsToDestroy)
            {
                if (t == null) continue;
                Destroy(t.gameObject);

                //FX
                RuntimeManager.PlayOneShot("event:/WallBreak", transform.position);
                // Spawn particle
                Instantiate(dustParticle, t.position, Quaternion.identity);
            }
        }

        spawnCutscene.Configure(cutsceneStartPos, lookAtTarget, cutsceneDuration, cutsceneHoldFraction, cutsceneBlendInDelay, cutsceneBlendInTime);
        CameraRefData.Instance.CameraCutsceneHandler.StartCutscene(spawnCutscene);

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
