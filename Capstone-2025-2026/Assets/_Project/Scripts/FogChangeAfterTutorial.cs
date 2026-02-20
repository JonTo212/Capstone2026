using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class FogChangeAfterTutorial : MonoBehaviour
{
    [SerializeField] private float timeToChange = 1.5f;
    [SerializeField] private PostProcessVolume volume;

    [SerializeField] private float newFogExponent = 0.002f;
    [SerializeField] private Color newFogColor = Color.white;

    [SerializeField] private bool playerHasEnteredTrigger = false;
    [SerializeField] private float timeElapsed = 0;
    private float currentFogExponent = 0;
    private Color currentFogColor = Color.white;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentFogColor = RenderSettings.fogColor;
        currentFogExponent = RenderSettings.fogDensity;
    }

    // Update is called once per frame
    void Update()
    {
        if(playerHasEnteredTrigger)
        {
            timeElapsed += Time.deltaTime;

            RenderSettings.fogColor = Color.Lerp(currentFogColor, newFogColor, Mathf.Clamp01(timeElapsed/timeToChange));
            RenderSettings.fogDensity = Mathf.Lerp(currentFogExponent, newFogExponent,Mathf.Clamp01(timeElapsed / timeToChange));
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
