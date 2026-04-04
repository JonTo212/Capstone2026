using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class UnloadSectionAfterDelay : MonoBehaviour
{
    [SerializeField] private GameObject[] unloadSections;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(UnloadNextFrame());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator UnloadNextFrame()
    {
        yield return new WaitForSeconds(0.5f);
        if (unloadSections.Length < 0)
        {
            foreach (GameObject section in unloadSections)
            {
                section.SetActive(false);
            }
        }
    }
}
