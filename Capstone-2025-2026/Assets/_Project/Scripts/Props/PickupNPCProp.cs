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


    private void Awake()
    {
        critterInstanceScript = GetComponent<CritterInstance>();

        base.Init();
        defaultLocalScale = transform.localScale;
    }

    public void OnCaptureStart(float captureDuration)
    {
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(Deflate(captureDuration, deflatedScale));

        DestroyAllAttachedTethers();

        RuntimeManager.PlayOneShot("event:/NPCSave", transform.position);

        //tell UI that you got a puff
        critterInstanceScript.BeRescued();
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
