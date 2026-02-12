using TMPro;
using UnityEngine;

public class CollectibleManager : MonoBehaviour
{
    
    public static CollectibleManager Instance { get; private set; }

    public TextMeshProUGUI coinCounter;
        
    public int coins = 0000;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        //DontDestroyOnLoad(gameObject);

    }

    public void CoinCollected()
    {
        coins++;
        coinCounter.text = coins.ToString();
    }
}
