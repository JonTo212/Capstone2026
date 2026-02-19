using FMODUnity;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class CoinPickup : MonoBehaviour
{
    public float pickupDistance = 10f;
    public float suctionSpeed = 20f;

    private Transform player;
    private SphereCollider trigger;
    private Coroutine collectionCoroutine;


    private void Start()
    {

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        trigger = GetComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = pickupDistance;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && collectionCoroutine == null)
        {
            collectionCoroutine = StartCoroutine(CollectCoroutine());
            CollectibleManager.Instance.CoinCollected();
        }
    }

    private IEnumerator CollectCoroutine()
    {
        trigger.enabled = false;
        while (player != null && Vector3.Distance(transform.position, player.position) > 0.2f)
        {
            transform.position = Vector3.MoveTowards(transform.position, player.position, suctionSpeed * Time.deltaTime);
            yield return null;
        }

        FinishCollect();
    }

    private void FinishCollect()
    {
        if (AudioManager.Instance != null)
        {
            //AudioManager.Instance.PlaySFX(AudioManager.Instance.Collection, 10, 1);
            RuntimeManager.PlayOneShot("event:/Collectible", transform.position);
        }

        Destroy(gameObject);
    }
}