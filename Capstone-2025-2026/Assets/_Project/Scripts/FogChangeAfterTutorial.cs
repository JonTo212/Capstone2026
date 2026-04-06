using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class FogChangeAfterTutorial : MonoBehaviour
{
    [SerializeField] private float timeToChange = 1.5f;
    [SerializeField] private PostProcessVolume volume;

    [SerializeField] private float newFogStartDistance = 10f;
    [SerializeField] private float newFogEndDistance = 1000f;
    [SerializeField] private Color newFogColor = Color.white;

    [SerializeField] private bool playerHasEnteredTrigger = false;
    [SerializeField] private float timeElapsed = 0;
    private float currentFogStartDistance = 0;
    private float currentFogEndDistance = 0;  
    private Color currentFogColor = Color.white;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentFogColor = RenderSettings.fogColor;
        currentFogStartDistance = RenderSettings.fogStartDistance;
        currentFogEndDistance = RenderSettings.fogEndDistance;
    }

    // Update is called once per frame
    void Update()
    {
        if(playerHasEnteredTrigger)
        {
            timeElapsed += Time.deltaTime;

            RenderSettings.fogColor = Color.Lerp(currentFogColor, newFogColor, Mathf.Clamp01(timeElapsed/timeToChange));
            RenderSettings.fogStartDistance = Mathf.Lerp(currentFogStartDistance, newFogStartDistance, Mathf.Clamp01(timeElapsed / timeToChange));
            RenderSettings.fogEndDistance = Mathf.Lerp(currentFogEndDistance, newFogEndDistance, Mathf.Clamp01(timeElapsed / timeToChange));
        }

        if(timeElapsed > timeToChange)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            playerHasEnteredTrigger = true;
        }
    }
}
