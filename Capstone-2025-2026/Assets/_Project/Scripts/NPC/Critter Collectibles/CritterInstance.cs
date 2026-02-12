using UnityEngine;

//Monobehaviour storing each unique Critter Type
//Component of Critter Collectible Prefab

public class CritterInstance : MonoBehaviour
{
    public CritterColllectible CritterCollectibleData;

    private int instID;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        SetUp();
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.CompareTag("Player"))
        {
            BeRescued();
        }
    }

    private void BeRescued ()
    {



    }

    private void SetUp()
    {
        GameObject critterVisual = Instantiate(CritterCollectibleData.CritterModelPrefab, this.transform);

        CollectibleManager.Instance.SetUpCritter(CritterCollectibleData.CritterStampSilhouette, CritterCollectibleData.CritterStampImg);
        instID = CollectibleManager.Instance.count;
        Debug.Log(instID);
    }
}
