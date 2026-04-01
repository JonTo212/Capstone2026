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


    public GameObject[] c_UIs;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (critterCount != CollectibleManager.Instance.critCount)
        {
            Populate();
        }
    }


    private void Populate()
    {

        
        critterCount = CollectibleManager.Instance.critCount;

        Debug.Log("Critters in level: " + critterCount);

        for (int i = 0; i < critterCount; i++)
        {
            //will be obsolete once Prefab is established
            c_UIs[i].SetActive(true);
            
            Image silh = c_UIs[i].GetComponent<Image>();
            silh.sprite = CollectibleManager.Instance.critLog[i].silSpr;
            

        }
    }

    public void UpdateVisual(Sprite newImg, int ID)
    {
        Image stamp = c_UIs[ID].GetComponent<Image>();
        stamp.sprite = newImg;


    }
}
