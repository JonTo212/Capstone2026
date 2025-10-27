using TMPro;
using UnityEngine;

public class animalCounter : MonoBehaviour
{
    public static int savedAnimals;
    private AnimalJar[] allAnimalJars;

    public TextMeshProUGUI counterText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        allAnimalJars = FindObjectsByType<AnimalJar>(FindObjectsSortMode.None);
    }

    // Update is called once per frame
    void Update()
    {
        counterText.text = (savedAnimals.ToString() + "/" + allAnimalJars.Length); 
    }
}
