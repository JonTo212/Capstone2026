using FMODUnity;
using System.Collections;
using UnityEngine;

public class LeverChallengeEndPoint : MonoBehaviour
{
    [SerializeField] private Transform challengeParent;
    [SerializeField] private Transform leverParent;
    [SerializeField] private Transform fakeLever;
    [SerializeField] private float timeToFallOver = 2f;
    [SerializeField] private float timeElapsed = 0f;
    [SerializeField] private bool challengeWasCompleted = false;
    [SerializeField] private BoxCollider collider;
    [SerializeField] private ParticleSystem explodeParticle;
    [SerializeField] private ParticleSystem dustParticle;

    [SerializeField] private bool JustCompleted = false;




    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        collider.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if(challengeWasCompleted)
        {

           
            timeElapsed = Mathf.Clamp01(timeElapsed + Time.deltaTime/timeToFallOver);

            float squaredTime = Mathf.Pow(timeElapsed, 2f);

            float lerpRotation = Mathf.Lerp(0f, 90f, squaredTime);

            challengeParent.transform.localRotation = Quaternion.Euler(lerpRotation, 0f, 0f);

            collider.enabled = true;


            if (!JustCompleted)
            {
                StartCoroutine(CompletedEffects());
                JustCompleted = true;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<LeverChallengeHandle>())
        {
            Destroy(other.gameObject);
            fakeLever.gameObject.SetActive(true);
            challengeWasCompleted = true;
        }
    }

    IEnumerator CompletedEffects()
    {
        //Completed Puzzle
        RuntimeManager.PlayOneShot("event:/Fanfare", transform.position);
        //RuntimeManager.PlayOneShot("event:/PuzzleComplete", transform.position);
        explodeParticle.Play();

        yield return new WaitForSeconds(timeToFallOver);

        //Hit Ground
        dustParticle.Play();
        RuntimeManager.PlayOneShot("event:/WallBreak", transform.position);
    }
}
