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
        Debug.Log("count: " + count);

        for (int i = 0; i < critLog.Count; i++)
        {
            if (critLog[i].critID == ID)
            {
                Debug.Log("Found critterID");
                spawner.UpdateVisual(critLog[i].stmSpr, critLog[i].critID);
            }


        }

        CritCollPopUp.RunAnims();

    }


    public void RegisterCritter (Sprite critterSilSprite, Sprite critterStampSprite, int ID)
    {
        count++;

        critLog.Add(new CritterCatalogue());
        critLog[count-1].critID = ID;
        critLog[count-1].silSpr = critterSilSprite;
        critLog[count-1].stmSpr = critterStampSprite;

    }

}
