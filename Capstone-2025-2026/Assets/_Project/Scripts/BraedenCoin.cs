using UnityEngine;

public class BraedenCoin : MonoBehaviour
{
    public float pickupDistance = 10f;
    public float suctionSpeed = 50f;
    private Transform player;
    public AudioManager audioManager;


    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        audioManager = GameObject.Find("AudioManager").GetComponent<AudioManager>();
    }


    private void OnTriggerEnter(Collider other)
    {
        //playsound
        audioManager.PlaySFX(audioManager.Collection, 10, 4f);

        //destroy
        Destroy(gameObject);
    }

    private void Update()
    {
        //get distance to player
        var distToPlayer = Vector3.Distance(transform.position, player.position);

        if (distToPlayer < pickupDistance)
        {
           transform.position = Vector3.MoveTowards(transform.position, player.position, Time.deltaTime * suctionSpeed);
        }
    }

}

