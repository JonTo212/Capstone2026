using TMPro;
using UnityEngine;

//The new big mama B)

public class CollectibleManager : MonoBehaviour
{
    
    public static CollectibleManager Instance { get; private set; }

    public TextMeshProUGUI coinCounter;
        
    public int coins = 0000;

    [HideInInspector]
    public Sprite[] silSprites;
    [HideInInspector]
    public Sprite[] stampSprites;

    private CritterCountSpawner spawner;
    private int numOfCritters = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        //DontDestroyOnLoad(gameObject);

        GameObject spawnerGO = GameObject.Find("CritterCountSpawner");
        spawner = spawnerGO.GetComponent<CritterCountSpawner>();

    }

    public void CoinCollected()
    {
        coins++;
        coinCounter.text = coins.ToString();
    }

    public void SetUpCritter (Sprite critterSilSprite)
    {
        numOfCritters++;
        
        spawner.SetUpSilUI(critterSilSprite);
    }
}
