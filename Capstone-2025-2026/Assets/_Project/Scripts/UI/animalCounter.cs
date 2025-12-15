using TMPro;
using UnityEngine;
using DG.Tweening;

public class animalCounter : MonoBehaviour
{
    public MySceneManager sceneManager;
    public static int savedAnimals;
    public BabyScript[] allAnimalJars;
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
        allAnimalJars = FindObjectsByType<BabyScript>(FindObjectsSortMode.None);
        currentAmount = allAnimalJars.Length;

        foreach(BabyScript baby in allAnimalJars)
        {
            baby.OnEnterBag += OnBabyEnterBag;
        }

        counterText.text = (savedAnimals.ToString() + "/" + allAnimalJars.Length);
        currentAmount = savedAnimals;

        TextOnScreen();
        Invoke(nameof(TextOffScreen), 5f);
    }

    // Update is called once per frame
    void Update()
    {
        if(savedAnimals == 4)
        {
            LoadEndScene();
        }
    }

    private void LoadEndScene()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        sceneManager.LoadNewScene(3);
    }

    private void OnBabyEnterBag()
    {
        savedAnimals++;
        counterText.text = (savedAnimals.ToString() + "/" + allAnimalJars.Length);
        TextOnScreen();
    }

    public void TextOnScreen()
    {
        gameObject.transform.DOMoveY(originalUIPosition, 2);
        Invoke(nameof(TextOffScreen), 5f);
    }

    public void TextOffScreen()
    {
        gameObject.transform.DOMoveY(-211,2);
    }
}
