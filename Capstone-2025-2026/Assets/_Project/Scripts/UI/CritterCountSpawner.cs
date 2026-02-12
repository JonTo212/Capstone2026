using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

//Populates critter UI based on how many are in the level

public class CritterCountSpawner : MonoBehaviour
{
    public GameObject critter_UI;

    private int critterCount = 0;
    private List<Sprite> silSprites = new List<Sprite>();


    private GameObject[] c_UIs;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        c_UIs = new GameObject[critterCount];
        Populate();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //Should be populating before Start()
    public void SetUpSilUI (Sprite silSprite)
    {
        //issue: need to add sprites as they come in and simultaneously set Array size without overwriting previous entries
        critterCount++;

        silSprites.Add(silSprite);

        
    }

    private void Populate()
    {

        for (int h = 0; h < critterCount; h++)
        {
            Debug.Log(silSprites[h]);
        }

        for (int i = 0; i < critterCount; i++)
        {
            Debug.Log("Populating");
            
            c_UIs[i] = Instantiate(critter_UI,this.transform);

            Vector3 placement = c_UIs[i].transform.position + new Vector3((i * 40) - 100, 0, 0);
            c_UIs[i].transform.position = placement;
            
            Image silh = c_UIs[i].GetComponent<Image>();
            silh.sprite = silSprites[i];
            
            //will be obsolete once Prefab is established
            c_UIs[i].SetActive(true);
            Debug.Log(silSprites[i]);
        }
    }
}
