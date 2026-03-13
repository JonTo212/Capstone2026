using System.Linq;
using FMODUnity;
using UnityEngine;

public class EndSequeenceTracker : MonoBehaviour
{

    public GameObject[] Supports;
    public GameObject[] EnabledArray;
    public GameObject[] DisabledArray;
    public Rigidbody bigPlat;
    public Rigidbody SpaceShip;

    // Update is called once per frame
    public void UpdateSupports(int targettedSupport)
    {
        Destroy(Supports[targettedSupport]);
        Supports[targettedSupport] = null;

        if (Supports[0] == null && Supports[1] == null && Supports[2] == null )
        {
            EndSequence();
        }
    } 

    public void EndSequence()
    {
        //Play Scene
        EnabledThings();
        DisabledThings();
        bigPlat.isKinematic = false;
        SpaceShip.useGravity = true;

        Debug.Log("THE END SCENE HAPPENED");
    }

    public void EnabledThings()
    {
        foreach(GameObject _object in EnabledArray){
            _object.SetActive(true);
        }
    }

    public void DisabledThings()
    {
        foreach(GameObject _object in DisabledArray){
            _object.SetActive(false);
        }
    }

}
