using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
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

    }

    // Update is called once per frame
    void Update()
    {
        if (critterCount != CollectibleManager.Instance.count)
        {
            Populate();
        }
    }


    private void Populate()
    {
        critterCount = CollectibleManager.Instance.count;
        c_UIs = new GameObject[critterCount];


        for (int i = 0; i < critterCount; i++)
        {
            
            c_UIs[i] = Instantiate(critter_UI,this.transform);

            //will be obsolete once Prefab is established
            c_UIs[i].SetActive(true);


            Vector3 placement = c_UIs[i].transform.position + new Vector3((i * 40) - 100, 0, 0);
            c_UIs[i].transform.position = placement;
            
            Image silh = c_UIs[i].GetComponent<Image>();
            silh.sprite = CollectibleManager.Instance.critLog[i].silSpr;
            

        }
    }
}
