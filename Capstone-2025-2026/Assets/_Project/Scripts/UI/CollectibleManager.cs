using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine;

//The new big mama B)

public class CollectibleManager : MonoBehaviour
{
    
    public static CollectibleManager Instance { get; private set; }

    public TextMeshProUGUI coinCounter;

    private CollectedCritterPopUp CritCollPopUp;
        
    public int coins = 0000;

    private CritterCountSpawner spawner;

    public class CritterCatalogue
    {
        public int critID { get; set; }
        public Sprite silSpr { get; set; }
        public Sprite stmSpr { get; set; }

    }
    public List<CritterCatalogue> critLog = new List<CritterCatalogue>();
    
    [HideInInspector]
    public int critCount = 0;
    [HideInInspector]
    public int critColCount = 0;

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

        GameObject PopUpGO = GameObject.Find("CritterCollectedPopUp");
        CritCollPopUp = PopUpGO.GetComponent<CollectedCritterPopUp>();

    }


    public void CoinCollected()
    {
        coins++;
        coinCounter.text = coins.ToString();
    }

    public void CritterCollected(int ID)
    {
        Debug.Log("count: " + critCount);

        for (int i = 0; i < critLog.Count; i++)
        {
            if (critLog[i].critID == ID)
            {
                Debug.Log("Found critterID");
                spawner.UpdateVisual(critLog[i].stmSpr, critLog[i].critID);
            }


        }

        critColCount++;

        CritCollPopUp.RunAnims();

    }


    public void RegisterCritter (Sprite critterSilSprite, Sprite critterStampSprite, int ID)
    {
        critCount++;

        critLog.Add(new CritterCatalogue());
        critLog[critCount -1].critID = ID;
        critLog[critCount -1].silSpr = critterSilSprite;
        critLog[critCount -1].stmSpr = critterStampSprite;

    }

    public void CallScore()
    {
        GameObject scoreGO = GameObject.Find("ScoreManager");
        scoreManager score = scoreGO.GetComponent<scoreManager>();
        score.EndGameSummary(coins, critColCount);

    }

}
