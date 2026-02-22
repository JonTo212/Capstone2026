using UnityEngine;

public class LevelLoaderUnloader : MonoBehaviour
{
    [SerializeField] private GameObject[] loadSections;
    [SerializeField] private GameObject[] unloadSections;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Player"))
        {
            foreach(GameObject gameObject in loadSections)
            {
                gameObject?.SetActive(true);
            }

            foreach(GameObject gameObject in unloadSections)
            {
                gameObject?.SetActive(false);
            }
        }
    }


}
