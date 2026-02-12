using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine;

//The new big mama B)

public class CollectibleManager : MonoBehaviour
{
    
    public static CollectibleManager Instance { get; private set; }

    public TextMeshProUGUI coinCounter;
        
    public int coins = 0000;

    private CritterCountSpawner spawner;

    public class CritterCatalogue
    {
        public int critID { get; set; }
        public Sprite silSpr { get; set; }
        public Sprite stmSpr { get; set; }

    }
    List<CritterCatalogue> critLog = new List<CritterCatalogue>();
    [HideInInspector]
    public int count = 0;

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

    public void CritterCollected(Sprite critterStampSprite)
    {

    }


    public void SetUpCritter (Sprite critterSilSprite, Sprite critterStampSprite)
    {

        critLog.Add(new CritterCatalogue());
        critLog[count].critID = count;
        critLog[count].silSpr = critterSilSprite;
        critLog[count].stmSpr = critterStampSprite;


        PopulateSetUp();
    }

    private void PopulateSetUp() 
    {
        spawner.SetUpSilUI(critLog[count].silSpr);
        count++;
    }

}
