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
    public List<CritterCatalogue> critLog = new List<CritterCatalogue>();
    
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

    public int GetID ()
    {
        return count;
    }

    public void CoinCollected()
    {
        coins++;
        coinCounter.text = coins.ToString();
    }

    public void CritterCollected(int ID)
    {

        for (int i = 0; i < critLog.Count; i++)
        {
            if (critLog[i].critID == ID)
            {
                spawner.UpdateVisual(critLog[i].stmSpr, i);
            }


        }


    }


    public void RegisterCritter (Sprite critterSilSprite, Sprite critterStampSprite)
    {
        count++;

        critLog.Add(new CritterCatalogue());
        critLog[count-1].critID = count;
        critLog[count-1].silSpr = critterSilSprite;
        critLog[count-1].stmSpr = critterStampSprite;

    }

}
