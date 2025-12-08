using TMPro;
using UnityEngine;
using DG.Tweening;

public class animalCounter : MonoBehaviour
{
    public static int savedAnimals;
    private AnimalJar[] allAnimalJars;
    public float originalUIPosition;
    public float currentAmount;

    public TextMeshProUGUI counterText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        originalUIPosition = transform.position.y;
    }

    void Start()
    {
        allAnimalJars = FindObjectsByType<AnimalJar>(FindObjectsSortMode.None);
        currentAmount = allAnimalJars.Length;
        Invoke(nameof(TextOffScreen), 5f);
    }

    // Update is called once per frame
    void Update()
    {
        if(savedAnimals != currentAmount)
        {
            TextOnScreen();
            counterText.text = (savedAnimals.ToString() + "/" + allAnimalJars.Length); 
            currentAmount = savedAnimals;
            Invoke(nameof(TextOffScreen), 5f);
        }
    }

    public void TextOnScreen()
    {
        gameObject.transform.DOMoveY(originalUIPosition, 2);
    }

    public void TextOffScreen()
    {
        gameObject.transform.DOMoveY(-211,2);
    }
}
