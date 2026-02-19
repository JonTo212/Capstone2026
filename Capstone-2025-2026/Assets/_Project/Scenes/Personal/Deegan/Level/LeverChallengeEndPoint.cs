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

            //play complete particle effect
            bool isplaying = explodeParticle.isPlaying;

            if (!isplaying)
            {
                explodeParticle.Play();
                isplaying = true;
            }

            //play dust landing particle
            bool isplaying2 = dustParticle.isPlaying;

            if (timeElapsed >= 1f && !isplaying2)
            {
                dustParticle.Play();
                isplaying2 = true; // Prevents re-triggering every frame
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
}
