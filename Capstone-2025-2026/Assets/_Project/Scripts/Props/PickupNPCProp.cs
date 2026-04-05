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

    [SerializeField] private DialogueTrigger dialogueTrigger;


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

        //tell UI that you got a puff
        critterInstanceScript.BeRescued();

        RuntimeManager.PlayOneShot("event:/NPCSave", transform.position);
        if (endTrack)
        {
            endTrack.EndSequence();
        }

        if (keyNPC && spawnCutscene != null)
        {
            //this is super jank right now, the cutscene blend delay has to be the same as the capture duration (capture clears the freeze and is an invoked event)
            //otherwise you can move during the popup
            spawnCutscene.Configure(cutsceneStartPos, lookAtTarget, cutsceneDuration, cutsceneHoldFraction, cutsceneBlendInDelay, cutsceneBlendInTime);
            CameraRefData.Instance.CameraCutsceneHandler.StartCutscene(spawnCutscene);
            Invoke("KeyNPCAction", cutsceneBlendInDelay);
        }
    }

    private void KeyNPCAction()
    {
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
                RuntimeManager.PlayOneShot("event:/PrisonBreak", transform.position);
                // Spawn particle
                Instantiate(dustParticle, t.position, Quaternion.identity);
            }
        }

        dialogueTrigger.CreateNPCDialogue();
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
