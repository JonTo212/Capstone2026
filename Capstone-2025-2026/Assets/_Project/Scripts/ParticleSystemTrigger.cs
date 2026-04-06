using UnityEngine;

public class ParticleSystemTrigger : MonoBehaviour
{
    [SerializeField] private ParticleSystem particles;

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            if (particles.isPlaying || particles == null) return;

            //particles.gameObject.transform.position = other.transform.position + other.transform.forward * 10f;
            particles.Play();
        }
    }
}
