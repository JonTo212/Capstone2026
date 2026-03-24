using FMODUnity;
using UnityEngine;

//Monobehaviour storing each unique Critter Type
//Component of Critter Collectible Prefab

public class CritterInstance : MonoBehaviour
{
    public CritterColllectible CritterCollectibleData;

    public int instID;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        SetUp();
        
    }


    private void OnTriggerEnter(Collider collision)
    {
        if (collision.transform.CompareTag("Player"))
        {
            BeRescued();
            Destroy(this.gameObject);//on collision becuase it can only destroy the physics based guys, destruction is handled by the player for the instant rod capture ones
            //Debug.Log("hit: " + instID);
        }
    }

    public void BeRescued ()
    {

        RuntimeManager.PlayOneShot("event:/NPCSave", transform.position);
        CollectibleManager.Instance.CritterCollected(instID);

        Debug.Log("saved");
        //Destroy(this.gameObject);

    }

    private void SetUp()
    {
        GameObject critterVisual = Instantiate(CritterCollectibleData.CritterModelPrefab, this.transform);

        CollectibleManager.Instance.RegisterCritter(CritterCollectibleData.CritterStampSilhouette, CritterCollectibleData.CritterStampImg, instID);
    }
}
