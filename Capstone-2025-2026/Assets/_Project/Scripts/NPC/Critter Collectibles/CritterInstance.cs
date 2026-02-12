using UnityEngine;

//Monobehaviour storing each unique Critter Type
//Component of Critter Collectible Prefab

public class CritterInstance : MonoBehaviour
{
    public CritterColllectible CritterCollectibleData;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
      
        GameObject critterVisual = Instantiate(CritterCollectibleData.CritterModelPrefab, this.transform);
        CollectibleManager.Instance.SetUpCritter(CritterCollectibleData.CritterStampSilhouette);
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
