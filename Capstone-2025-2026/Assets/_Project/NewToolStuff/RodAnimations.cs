using UnityEngine;

public class RodAnimations : MonoBehaviour
{
    public static RodAnimations Instance;
    [SerializeField] private PlayerActions playerActions;
    public GameObject Handle;
    public float rotationAmount = 5f;

    //sound
    [SerializeField] private AudioSource audioSource; // The AudioSource component
    [SerializeField] private AudioClip[] clips;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        float scrollValue = playerActions.GetDPadScrollValue();

        if (scrollValue != 0)
        {
            Debug.Log("Scroll value: " + scrollValue);
            
            Handle.transform.Rotate(Vector3.left, scrollValue * rotationAmount);
            PlayRandomSound();
        }

    }

    public void PlayRandomSound()
    {
        //prevents game from crashing if i forgot to add clips
        if (clips.Length == 0) return;

        // Pick a random clip
        int randomIndex = Random.Range(0, clips.Length);

        // Play the clip as a one-shot
        audioSource.PlayOneShot(clips[randomIndex]);

    }
}
