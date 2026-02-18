using UnityEngine;

//Monobehaviour storing each unique Critter Type
//Component of Critter Collectible Prefab

public class CritterInstance : MonoBehaviour
{
    public CritterColllectible CritterCollectibleData;

    [HideInInspector]
    public int instID;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        SetUp();
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.transform.CompareTag("Player"))
        {
            BeRescued();
            //Debug.Log("hit: " + instID);
        }
    }

    private void BeRescued ()
    {

        CollectibleManager.Instance.CritterCollected(instID);

    }

    private void SetUp()
    {
        GameObject critterVisual = Instantiate(CritterCollectibleData.CritterModelPrefab, this.transform);

        CollectibleManager.Instance.RegisterCritter(CritterCollectibleData.CritterStampSilhouette, CritterCollectibleData.CritterStampImg);
        instID = CollectibleManager.Instance.GetID();
    }
}
